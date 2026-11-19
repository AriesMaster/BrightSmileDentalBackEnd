
using BrightSmileDEntal.Constants;
using BrightSmileDEntal.Data;
using BrightSmileDEntal.DTOs.Auth;
using BrightSmileDEntal.Models;
using BrightSmileDEntal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace BrightSmileDEntal.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtService _jwt;
        private readonly EmailService _email;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            ApplicationDbContext context,
            JwtService jwt,
            EmailService email,
            IConfiguration config,
            ILogger<AuthController> logger)
        {
            _context = context;
            _jwt = jwt;
            _email = email;
            _config = config;
            _logger = logger;
        }

        // ==========================================
        // REGISTER PATIENT
        // ==========================================

        /// <summary>
        /// Registers a new patient account and
        /// creates the associated patient profile.
        ///
        /// Public registration always assigns
        /// the Patient role.
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromBody] RegisterDto dto)
        {
            // --------------------------------------
            // NORMALIZE ACCOUNT INFORMATION
            // --------------------------------------

            var email = dto.Email.Trim().ToLowerInvariant();

            var firstName = dto.FirstName.Trim();
            var lastName = dto.LastName.Trim();

            var phoneNumber =
                dto.PhoneNumber?.Trim() ?? string.Empty;

            var gender =
                dto.Gender?.Trim() ?? string.Empty;

            // --------------------------------------
            // VALIDATE PERSONAL INFORMATION
            // --------------------------------------

            if (firstName.Length < 2 ||
                lastName.Length < 2)
            {
                return BadRequest(new
                {
                    message =
                        "First name and last name must contain at least 2 characters."
                });
            }

            if (dto.DateOfBirth.HasValue &&
                dto.DateOfBirth.Value.Date > DateTime.UtcNow.Date)
            {
                return BadRequest(new
                {
                    message =
                        "Date of birth cannot be in the future."
                });
            }

            // --------------------------------------
            // CHECK FOR EXISTING EMAIL
            // --------------------------------------

            var emailExists = await _context.Users
                .AnyAsync(u => u.Email.ToLower() == email);

            if (emailExists)
            {
                return Conflict(new
                {
                    message =
                        "An account with this email address already exists."
                });
            }

            // --------------------------------------
            // CREATE USER
            // --------------------------------------

            var user = new User
            {
                Email = email,

                PasswordHash =
                    BCrypt.Net.BCrypt.HashPassword(
                        dto.Password
                    ),

                Role = UserRoles.Patient
            };

            // --------------------------------------
            // CREATE PATIENT PROFILE
            // --------------------------------------

            var profile = new Profile
            {
                User = user,

                FirstName = firstName,
                LastName = lastName,

                PhoneNumber = phoneNumber,

                DateOfBirth = dto.DateOfBirth,

                Gender = gender
            };

            // --------------------------------------
            // SAVE USER AND PROFILE TOGETHER
            // --------------------------------------

            // EF Core tracks the related User
            // through the Profile navigation.
            //
            // SaveChangesAsync inserts both
            // records in one operation.

            _context.Profiles.Add(profile);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // The database's unique email
                // constraint protects against
                // simultaneous registrations.

                _logger.LogWarning(
                    ex,
                    "Database rejected patient registration."
                );

                return Conflict(new
                {
                    message =
                        "Unable to register this account. The email may already be in use."
                });
            }

            // --------------------------------------
            // LOG SUCCESSFUL REGISTRATION
            // --------------------------------------

            _logger.LogInformation(
                "Patient account registered successfully. UserId: {UserId}",
                user.Id
            );

            // --------------------------------------
            // RESPONSE
            // --------------------------------------

            return Ok(new
            {
                message =
                    "Patient account created successfully. Please log in."
            });
        }

        // ==========================================
        // FORGOT PASSWORD
        // ==========================================

        /// <summary>
        /// Generates a secure password reset token
        /// and sends a reset link to the user.
        /// </summary>
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(
            PasswordResetRequestDto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();

            var user = await _context.Users
                .FirstOrDefaultAsync(
                    u => u.Email.ToLower() == email
                );

            // Prevent account enumeration.
            if (user == null)
            {
                return Ok(
                    "If that email exists, a password reset link has been sent."
                );
            }

            // Generate a cryptographically
            // secure random token.
            var token = Convert.ToBase64String(
                RandomNumberGenerator.GetBytes(64)
            );

            // Store only the token hash.
            user.ResetToken = HashResetToken(token);

            user.ResetTokenExpiry =
                DateTime.UtcNow.AddHours(1);

            await _context.SaveChangesAsync();

            // Build frontend reset link.
            var clientUrl =
                _config["App:ClientUrl"]
                ?? "http://localhost:4200";

            var resetLink =
                $"{clientUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(token)}";

            var subject = "Password reset request";

            var body =
                "<p>Click the link below to reset your password. " +
                "The link will expire in 1 hour.</p>" +
                $"<p><a href=\"{resetLink}\">Reset your password</a></p>";

            try
            {
                _email.SendEmail(
                    user.Email,
                    subject,
                    body
                );

                _logger.LogInformation(
                    "Password reset email sent for {Email}",
                    email
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send password reset email for {Email}",
                    email
                );
            }

            return Ok(
                "If that email exists, a password reset link has been sent."
            );
        }

        // ==========================================
        // RESET PASSWORD
        // ==========================================

        /// <summary>
        /// Validates the reset token and
        /// updates the user's password.
        /// </summary>
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(
            ResetPasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Token) ||
                string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                return BadRequest(new
                {
                    message = "Invalid token or password."
                });
            }

            var hashedToken =
                HashResetToken(dto.Token);

            var user = await _context.Users
                .FirstOrDefaultAsync(
                    u => u.ResetToken == hashedToken
                );

            if (user == null ||
                user.ResetTokenExpiry == null ||
                user.ResetTokenExpiry <= DateTime.UtcNow)
            {
                return BadRequest(new
                {
                    message = "Invalid or expired token."
                });
            }

            // Hash the new password.
            user.PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(
                    dto.NewPassword
                );

            // Invalidate the reset token.
            user.ResetToken = null;
            user.ResetTokenExpiry = null;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Password successfully reset for {Email}",
                user.Email
            );

            return Ok(new
            {
                message =
                    "Password has been reset successfully."
            });
        }

        // ==========================================
        // LOGIN
        // ==========================================

        /// <summary>
        /// Authenticates a user and returns
        /// a signed JWT.
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login(
            LoginDto dto)
        {
            var email =
                dto.Email.Trim().ToLowerInvariant();

            var user = await _context.Users
                .FirstOrDefaultAsync(
                    u => u.Email.ToLower() == email
                );

            if (user == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid credentials."
                });
            }

            var validPassword =
                BCrypt.Net.BCrypt.Verify(
                    dto.Password,
                    user.PasswordHash
                );

            if (!validPassword)
            {
                return Unauthorized(new
                {
                    message = "Invalid credentials."
                });
            }

            // Generate JWT with user identity
            // and role claims.
            var token = _jwt.GenerateToken(user);

            _logger.LogInformation(
                "User authenticated successfully. UserId: {UserId}, Role: {Role}",
                user.Id,
                user.Role
            );

            return Ok(new
            {
                token,
                email = user.Email,
                role = user.Role
            });
        }

        // ==========================================
        // HELPER METHODS
        // ==========================================

        /// <summary>
        /// Hashes a password reset token
        /// using SHA-256.
        /// </summary>
        private static string HashResetToken(
            string token)
        {
            var tokenBytes =
                Encoding.UTF8.GetBytes(token);

            var hashBytes =
                SHA256.HashData(tokenBytes);

            return Convert.ToHexString(hashBytes);
        }
    }
}
