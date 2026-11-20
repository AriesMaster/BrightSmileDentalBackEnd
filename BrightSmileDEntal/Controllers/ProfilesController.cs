
using System.Security.Claims;
using BrightSmileDEntal.Data;
using BrightSmileDEntal.DTOs.Profile;
using BrightSmileDEntal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BrightSmileDEntal.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProfilesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProfilesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // GET CURRENT USER PROFILE
        // GET /api/Profiles/me
        // ==========================================

        [HttpGet("me")]
        public async Task<ActionResult<ProfileDto>> GetMyProfile()
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid user identity."
                });
            }

            var profile = await _context.Profiles
                .AsNoTracking()
                .Where(p => p.UserId == userId.Value)
                .Select(p => new ProfileDto
                {
                    Id = p.Id,
                    UserId = p.UserId,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    PhoneNumber = p.PhoneNumber,
                    DateOfBirth = p.DateOfBirth,
                    Gender = p.Gender
                })
                .FirstOrDefaultAsync();

            if (profile == null)
            {
                return NotFound(new
                {
                    message = "Profile not found. Complete your profile to continue."
                });
            }

            return Ok(profile);
        }

        // ==========================================
        // CREATE OR UPDATE CURRENT USER PROFILE
        // PUT /api/Profiles/me
        // ==========================================

        [HttpPut("me")]
        public async Task<ActionResult<ProfileDto>> UpdateMyProfile(
            [FromBody] UpdateProfileDto dto)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid user identity."
                });
            }

            // ======================================
            // VALIDATE PERSONAL INFORMATION
            // ======================================

            var firstName = dto.FirstName?.Trim()
                ?? string.Empty;

            var lastName = dto.LastName?.Trim()
                ?? string.Empty;

            var phoneNumber = dto.PhoneNumber?.Trim()
                ?? string.Empty;

            var gender = dto.Gender?.Trim()
                ?? string.Empty;

            if (firstName.Length < 2 ||
                firstName.Length > 100)
            {
                return BadRequest(new
                {
                    message = "First name must contain between 2 and 100 characters."
                });
            }

            if (lastName.Length < 2 ||
                lastName.Length > 100)
            {
                return BadRequest(new
                {
                    message = "Last name must contain between 2 and 100 characters."
                });
            }

            if (phoneNumber.Length > 20)
            {
                return BadRequest(new
                {
                    message = "Phone number cannot exceed 20 characters."
                });
            }

            if (gender.Length > 30)
            {
                return BadRequest(new
                {
                    message = "Gender cannot exceed 30 characters."
                });
            }

            // ======================================
            // NORMALIZE DATE OF BIRTH TO UTC
            // ======================================

            DateTime? dateOfBirth = null;

            if (dto.DateOfBirth.HasValue)
            {
                var suppliedDate = dto.DateOfBirth.Value;

                // Date of birth represents a calendar date,
                // not a time-of-day or timezone-specific event.
                // Normalize the date for PostgreSQL timestamptz.
                dateOfBirth = DateTime.SpecifyKind(
                    suppliedDate.Date,
                    DateTimeKind.Utc
                );

                if (dateOfBirth.Value.Date > DateTime.UtcNow.Date)
                {
                    return BadRequest(new
                    {
                        message = "Date of birth cannot be in the future."
                    });
                }
            }

            // ======================================
            // VERIFY USER ACCOUNT
            // ======================================

            var userExists = await _context.Users
                .AnyAsync(u => u.Id == userId.Value);

            if (!userExists)
            {
                return NotFound(new
                {
                    message = "User account not found."
                });
            }

            // ======================================
            // RETRIEVE EXISTING PROFILE
            // ======================================

            var profile = await _context.Profiles
                .FirstOrDefaultAsync(
                    p => p.UserId == userId.Value
                );

            var isNewProfile = profile == null;

            // ======================================
            // CREATE PROFILE IF MISSING
            // ======================================

            if (profile == null)
            {
                profile = new Profile
                {
                    UserId = userId.Value
                };

                _context.Profiles.Add(profile);
            }

            // ======================================
            // UPDATE PROFILE FIELDS
            // ======================================

            profile.FirstName = firstName;
            profile.LastName = lastName;
            profile.PhoneNumber = phoneNumber;
            profile.DateOfBirth = dateOfBirth;
            profile.Gender = gender;

            // ======================================
            // SAVE CHANGES
            // ======================================

            await _context.SaveChangesAsync();

            // ======================================
            // RETURN PROFILE DTO
            // ======================================

            var result = new ProfileDto
            {
                Id = profile.Id,
                UserId = profile.UserId,
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                PhoneNumber = profile.PhoneNumber,
                DateOfBirth = profile.DateOfBirth,
                Gender = profile.Gender
            };

            if (isNewProfile)
            {
                return CreatedAtAction(
                    nameof(GetMyProfile),
                    result
                );
            }

            return Ok(result);
        }

        // ==========================================
        // JWT USER ID HELPER
        // ==========================================

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );

            return int.TryParse(userIdClaim, out var userId)
                ? userId
                : null;
        }
    }
}
