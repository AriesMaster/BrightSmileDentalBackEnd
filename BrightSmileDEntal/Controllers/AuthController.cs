
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
    /// <summary>
    /// Handles patient registration, authentication, and password recovery.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtService _jwt;
        private readonly EmailService _email;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthController> _logger;

        /// <summary>
        /// Initializes the authentication controller.
        /// </summary>
        /// <param name="context">The application database context.</param>
        /// <param name="jwt">The service responsible for issuing JWTs.</param>
        /// <param name="email">The service used to send password reset emails.</param>
        /// <param name="config">Application configuration.</param>
        /// <param name="logger">The authentication controller logger.</param>
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


        /// <summary>
        /// Registers a new patient account.
        /// </summary>
        /// <remarks>
        /// Creates a patient user and associated profile. Email addresses are normalized and passwords are stored as BCrypt hashes.
        /// </remarks>
        /// <response code="200">Request completed successfully.</response>
        /// <response code="400">The request is invalid.</response>
        /// <response code="409">An account or database record conflicts with the registration.</response>
        /// <response code="500">Registration could not be completed.</response>
        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromBody] RegisterDto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();
            var firstName = dto.FirstName.Trim();
            var lastName = dto.LastName.Trim();

            var phoneNumber =
                dto.PhoneNumber?.Trim() ?? string.Empty;

            var gender =
                dto.Gender?.Trim() ?? string.Empty;

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

            var user = new User
            {
                Email = email,

                PasswordHash =
                    BCrypt.Net.BCrypt.HashPassword(dto.Password),

                Role = UserRoles.Patient
            };

            var profile = new Profile
            {
                User = user,
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = phoneNumber,
                DateOfBirth = dateOfBirth,
                Gender = gender
            };

            // EF Core persists the related user and profile in a single SaveChanges call.
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


        /// <summary>
        /// Requests a password reset link.
        /// </summary>
        /// <remarks>
        /// Returns the same success message whether or not the email address belongs to an account. Reset tokens are cryptographically generated, stored as hashes, and expire after one hour. Email delivery failures are logged.
        /// </remarks>
        /// <response code="200">Request completed successfully.</response>
        /// <response code="400">The request is invalid.</response>
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(
            PasswordResetRequestDto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();

            var user = await _context.Users
                .FirstOrDefaultAsync(
                    u => u.Email.ToLower() == email
                );

            // Use a generic response to avoid account enumeration.
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

            // Persist only the token hash so a database leak does not expose usable reset links.
            user.ResetToken = HashResetToken(token);

            user.ResetTokenExpiry =
                DateTime.UtcNow.AddHours(1);

            await _context.SaveChangesAsync();

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


        /// <summary>
        /// Resets an account password using a valid reset token.
        /// </summary>
        /// <remarks>
        /// The reset token must be unexpired. On success, the password is rehashed and the reset token is invalidated.
        /// </remarks>
        /// <response code="200">Request completed successfully.</response>
        /// <response code="400">The request is invalid.</response>
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


        /// <summary>
        /// Authenticates a user and issues a JWT.
        /// </summary>
        /// <remarks>
        /// Returns a bearer token and the authenticated user's email and role when credentials are valid.
        /// </remarks>
        /// <response code="200">Request completed successfully.</response>
        /// <response code="400">The request is invalid.</response>
        /// <response code="401">The credentials are invalid.</response>
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
