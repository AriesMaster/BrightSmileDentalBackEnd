
using BrightSmileDEntal.Constants;
using BrightSmileDEntal.Data;
using BrightSmileDEntal.DTOs.Auth;
using BrightSmileDEntal.Models;
using BrightSmileDEntal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
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

        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromBody] RegisterDto dto)
        {
            // Normalize registration details.
            var email = dto.Email.Trim().ToLowerInvariant();
            var firstName = dto.FirstName.Trim();
            var lastName = dto.LastName.Trim();

            var phoneNumber =
                dto.PhoneNumber?.Trim() ?? string.Empty;

            var gender =
                dto.Gender?.Trim() ?? string.Empty;

            // Validate personal information.
            if (firstName.Length < 2 ||
                lastName.Length < 2)
            {
                return BadRequest(new
                {
                    message =
                        "First name and last name must contain at least 2 characters."
                });
            }

            // Normalize the date of birth to UTC.
            // PostgreSQL timestamptz requires UTC DateTime values.
            DateTime? dateOfBirth = null;

            if (dto.DateOfBirth.HasValue)
            {
                dateOfBirth = DateTime.SpecifyKind(
                    dto.DateOfBirth.Value.Date,
                    DateTimeKind.Utc
                );

                if (dateOfBirth.Value.Date > DateTime.UtcNow.Date)
                {
                    return BadRequest(new
                    {
                        message =
                            "Date of birth cannot be in the future."
                    });
                }
            }

            // Check whether the email already exists.
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

            // Create the patient user.
            var user = new User
            {
                Email = email,

                PasswordHash =
                    BCrypt.Net.BCrypt.HashPassword(dto.Password),

                Role = UserRoles.Patient
            };

            // Create the associated profile.
            var profile = new Profile
            {
                User = user,
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = phoneNumber,
                DateOfBirth = dateOfBirth,
                Gender = gender
            };

            // EF Core tracks the relationship and saves
            // the User and Profile in one transaction.
            _context.Profiles.Add(profile);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
                when (ex.InnerException is PostgresException pgEx &&
                      pgEx.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                var pgError =
                    (PostgresException)ex.InnerException!;

                _logger.LogWarning(
                    ex,
                    "Unique constraint violation during registration. Constraint: {Constraint}",
                    pgError.ConstraintName
                );

                if (pgError.ConstraintName == "IX_Users_Email")
                {
                    return Conflict(new
                    {
                        message =
                            "An account with this email address already exists."
                    });
                }

                return Conflict(new
                {
                    message =
                        "Registration conflicts with an existing database record."
                });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Database error during patient registration."
                );

                return StatusCode(500, new
                {
                    message =
                        "Unable to complete registration due to a database error."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error during patient registration."
                );

                return StatusCode(500, new
                {
                    message =
                        "An unexpected error occurred during registration."
                });
            }

            _logger.LogInformation(
                "Patient account registered successfully. UserId: {UserId}",
                user.Id
            );

            return Ok(new
            {
                message =
                    "Patient account created successfully. Please log in."
            });
        }

        // ==========================================
        // FORGOT PASSWORD
        // ==========================================

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(
            PasswordResetRequestDto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();

            var user = await _context.Users
                .FirstOrDefaultAsync(
                    u => u.Email.ToLower() == email
                );

            // Avoid revealing whether an account exists.
            if (user == null)
            {
                return Ok(
                    "If that email exists, a password reset link has been sent."
                );
            }

            // Generate a cryptographically secure token.
            var token = Convert.ToBase64String(
                RandomNumberGenerator.GetBytes(64)
            );

            // Store only the hashed token.
            user.ResetToken = HashResetToken(token);

            user.ResetTokenExpiry =
                DateTime.UtcNow.AddHours(1);

            await _context.SaveChangesAsync();

            // Build the frontend reset link.
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

            // Update the password securely.
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

            // Generate the JWT token.
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
