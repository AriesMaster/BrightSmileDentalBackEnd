using BrightSmileDEntal.Data;
using BrightSmileDEntal.DTOs.Auth;
using BrightSmileDEntal.Models;
using BrightSmileDEntal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using System;

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

        public AuthController(ApplicationDbContext context, JwtService jwt, EmailService email, IConfiguration config)
        {
            _context = context;
            _jwt = jwt;
            _email = email;
            _config = config;
        }

        // REGISTER
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            // check if email exists
            if (await _context.Users.AnyAsync(x => x.Email == dto.Email))
            {
                return BadRequest("Email already exists.");
            }

            // create user
            var user = new User
            {
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = dto.Role
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok("User registered successfully.");
        }

        // FORGOT PASSWORD - sends a reset link to the user's email if it exists
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(PasswordResetRequestDto dto)
        {

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

            // Do not reveal whether the email exists; return OK regardless to avoid account enumeration.
            if (user == null)
                return Ok("If that email exists, a password reset link has been sent.");

            // generate a random token
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            user.ResetToken = token;
            user.ResetTokenExpiry = DateTime.UtcNow.AddHours(1);

            await _context.SaveChangesAsync();

            // Build reset link: client handles token via route like /reset-password?token=...
            var clientUrl = _config["App:ClientUrl"] ?? "http://localhost:4200";
            var resetLink = $"{clientUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(token)}";

            var subject = "Password reset request";
            var body = $"<p>Click the link below to reset your password. The link will expire in 1 hour.</p>" +
                       $"<p><a href=\"{resetLink}\">Reset your password</a></p>";

            try
            {
                _email.SendEmail(user.Email, subject, body);
            }
            catch
            {
                // Log or handle the email failure as needed. Still return OK to the caller.
            }

            return Ok("If that email exists, a password reset link has been sent.");
        }

        // RESET PASSWORD - accepts token and new password
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Token) || string.IsNullOrWhiteSpace(dto.NewPassword))
                return BadRequest("Invalid token or password.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.ResetToken == dto.Token);

            if (user == null || user.ResetTokenExpiry == null || user.ResetTokenExpiry < DateTime.UtcNow)
                return BadRequest("Invalid or expired token.");

            // update password and clear token
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.ResetToken = null;
            user.ResetTokenExpiry = null;

            await _context.SaveChangesAsync();

            return Ok("Password has been reset successfully.");
        }


        // LOGIN
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            // find user
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Email == dto.Email);

            if (user == null)
                return Unauthorized("Invalid credentials");

            // verify password
            bool validPassword = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);

            if (!validPassword)
                return Unauthorized("Invalid credentials");

            // generate JWT token
            var token = _jwt.GenerateToken(user);

            return Ok(new
            {
                token,
                email = user.Email,
                role = user.Role
            });
        }
    }
}