
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

        // --------------------------------------------------
        // REGISTER
        // Public registration always creates a Patient.
        // Admin and Dentist accounts must be created through
        // an authorized administrative workflow.
        // --------------------------------------------------

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();

            var emailExists = await _context.Users
                .AnyAsync(x => x.Email.ToLower() == email);

            if (emailExists)
            {
                return BadRequest(new
                {
                    message = "Email already exists."
                });
            }

            var user = new User
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = UserRoles.Patient
            };

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "New patient account registered: {Email}",
                email
            );

            return Ok(new
            {
                message = "User registered successfully."
            });
        }

        // --------------------------------------------------
        // FORGOT PASSWORD
        // Generates a secure, single-use password reset token.
        // Only the hashed token is stored in the database.
        // --------------------------------------------------

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(
            PasswordResetRequestDto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

            // Prevent account enumeration.
            if (user == null)
            {
                return Ok(
                    "If that email exists, a password reset link has been sent."
                );
            }

            // Generate a cryptographically secure random token.
            var token = Convert.ToBase64String(
                RandomNumberGenerator.GetBytes(64)
            );

            // Store only the SHA-256 hash of the token.
            user.ResetToken = HashResetToken(token);
            user.ResetTokenExpiry = DateTime.UtcNow.AddHours(1);

            await _context.SaveChangesAsync();

            // Build frontend reset link.
            var clientUrl = _config["App:ClientUrl"]
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
                _email.SendEmail(user.Email, subject, body);

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

        // --------------------------------------------------
        // RESET PASSWORD
        // Validates the reset token and updates the password.
        // --------------------------------------------------

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

            // Hash the supplied token before database lookup.
            var hashedToken = HashResetToken(dto.Token);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.ResetToken == hashedToken);

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
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(
                dto.NewPassword
            );

            // Invalidate the reset token after successful use.
            user.ResetToken = null;
            user.ResetTokenExpiry = null;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Password successfully reset for {Email}",
                user.Email
            );

            return Ok(new
            {
                message = "Password has been reset successfully."
            });
        }

        // --------------------------------------------------
        // LOGIN
        // Authenticates the user and generates a JWT.
        // --------------------------------------------------

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();

            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Email.ToLower() == email);

            if (user == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid credentials."
                });
            }

            var validPassword = BCrypt.Net.BCrypt.Verify(
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

            // Generate JWT containing user identity and role.
            var token = _jwt.GenerateToken(user);

            _logger.LogInformation(
                "User authenticated successfully: {Email}, Role: {Role}",
                user.Email,
                user.Role
            );

            return Ok(new
            {
                token,
                email = user.Email,
                role = user.Role
            });
        }

        // --------------------------------------------------
        // HELPER METHODS
        // --------------------------------------------------

        private static string HashResetToken(string token)
        {
            var tokenBytes = Encoding.UTF8.GetBytes(token);

            var hashBytes = SHA256.HashData(tokenBytes);

            return Convert.ToHexString(hashBytes);
        }
    }
}
