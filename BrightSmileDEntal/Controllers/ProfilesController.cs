
using System.Security.Claims;
using BrightSmileDEntal.Data;
using BrightSmileDEntal.DTOs.Profile;
using BrightSmileDEntal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BrightSmileDEntal.Controllers
{
    /// <summary>
    /// Manages profile information for the authenticated user.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProfilesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Initializes the profile controller.
        /// </summary>
        /// <param name="context">The application database context.</param>
        public ProfilesController(ApplicationDbContext context)
        {
            _context = context;
        }


        /// <summary>
        /// Gets the authenticated user's profile.
        /// </summary>
        /// <remarks>
        /// Returns the profile associated with the user identifier in the JWT.
        /// </remarks>
        /// <response code="200">Profile returned or updated successfully.</response>
        /// <response code="401">Authentication or a valid user identity is required.</response>
        /// <response code="404">The profile or user account was not found.</response>
        [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
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
        /// Creates or updates the authenticated user's profile.
        /// </summary>
        /// <remarks>
        /// Validates personal details and stores dates of birth as UTC-normalized calendar dates for PostgreSQL compatibility. Returns 201 when a profile is created and 200 when an existing profile is updated.
        /// </remarks>
        /// <response code="200">Profile returned or updated successfully.</response>
        /// <response code="201">A new profile was created.</response>
        /// <response code="400">Profile information is invalid.</response>
        /// <response code="401">Authentication or a valid user identity is required.</response>
        /// <response code="404">The profile or user account was not found.</response>
        [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status201Created)]
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
                .FirstOrDefaultAsync(
                    p => p.UserId == userId.Value
                );

            var isNewProfile = profile == null;


            if (profile == null)
            {
                profile = new Profile
                {
                    UserId = userId.Value
                };

                _context.Profiles.Add(profile);
            }


            profile.FirstName = firstName;
            profile.LastName = lastName;
            profile.PhoneNumber = phoneNumber;
            profile.DateOfBirth = dateOfBirth;
            profile.Gender = gender;


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
