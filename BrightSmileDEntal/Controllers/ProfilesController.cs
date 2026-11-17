
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

        /// <summary>
        /// Retrieves the profile associated with the authenticated user.
        /// </summary>
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

        /// <summary>
        /// Creates or updates the profile belonging to the authenticated user.
        /// </summary>
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

            var userExists = await _context.Users
                .AnyAsync(u => u.Id == userId.Value);

            if (!userExists)
            {
                return NotFound(new
                {
                    message = "User account not found."
                });
            }

            var profile = await _context.Profiles
                .FirstOrDefaultAsync(p => p.UserId == userId.Value);

            var isNewProfile = profile == null;

            // Create a profile when the account has no existing profile record.
            if (profile == null)
            {
                profile = new Profile
                {
                    UserId = userId.Value
                };

                _context.Profiles.Add(profile);
            }

            profile.FirstName = dto.FirstName.Trim();
            profile.LastName = dto.LastName.Trim();
            profile.PhoneNumber = dto.PhoneNumber?.Trim() ?? string.Empty;
            profile.DateOfBirth = dto.DateOfBirth;
            profile.Gender = dto.Gender?.Trim() ?? string.Empty;

            await _context.SaveChangesAsync();

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

        // Resolve user identity from the validated JWT rather than request data.
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
