
using System.Security.Claims;
using BrightSmileDEntal.Constants;
using BrightSmileDEntal.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BrightSmileDEntal.Controllers
{
    /// <summary>
    /// Provides authenticated account information and administrative patient records.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Initializes the users controller.
        /// </summary>
        /// <param name="context">The application database context.</param>
        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets the authenticated user's account details.
        /// </summary>
        /// <remarks>
        /// Returns the user identifier, email address, and role from the account associated with the JWT.
        /// </remarks>
        /// <response code="200">The request succeeded.</response>
        /// <response code="401">Authentication is required or the user identity is invalid.</response>
        /// <response code="404">The user or patient was not found.</response>
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var claim = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(claim, out var userId))
            {
                return Unauthorized(new
                {
                    message = "Invalid user identity."
                });
            }

            var user = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.Role
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound(new
                {
                    message = "User not found."
                });
            }

            return Ok(user);
        }

        /// <summary>
        /// Lists patient accounts and appointment counts.
        /// </summary>
        /// <remarks>
        /// Administrator access is required. Returns patients ordered by email, including profile contact details, total appointments, and upcoming pending or confirmed appointments.
        /// </remarks>
        /// <response code="200">The request succeeded.</response>
        /// <response code="401">Authentication is required or the user identity is invalid.</response>
        /// <response code="403">Administrator access is required.</response>
        [HttpGet("patients")]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> GetPatients()
        {
            var patients = await _context.Users
                .AsNoTracking()
                .Where(u => u.Role == UserRoles.Patient)
                .OrderBy(u => u.Email)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    FirstName = u.Profile != null
                        ? u.Profile.FirstName : "",
                    LastName = u.Profile != null
                        ? u.Profile.LastName : "",
                    PhoneNumber = u.Profile != null
                        ? u.Profile.PhoneNumber : "",
                    AppointmentCount = _context.Appointments
                        .Count(a => a.PatientId == u.Id),
                    UpcomingAppointments = _context.Appointments
                        .Count(a =>
                            a.PatientId == u.Id &&
                            (a.Status == "Pending" ||
                             a.Status == "Confirmed") &&
                            a.AppointmentDate > DateTime.UtcNow)
                })
                .ToListAsync();

            return Ok(patients);
        }

        /// <summary>
        /// Gets a patient record and appointment history.
        /// </summary>
        /// <remarks>
        /// Administrator access is required. Returns patient profile details and appointments ordered from newest to oldest.
        /// </remarks>
        /// <response code="200">The request succeeded.</response>
        /// <response code="401">Authentication is required or the user identity is invalid.</response>
        /// <response code="403">Administrator access is required.</response>
        /// <response code="404">The user or patient was not found.</response>
        [HttpGet("patients/{id:int}")]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> GetPatient(int id)
        {
            var patient = await _context.Users
                .AsNoTracking()
                .Where(u =>
                    u.Id == id &&
                    u.Role == UserRoles.Patient)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.Role,
                    Profile = u.Profile == null ? null : new
                    {
                        u.Profile.FirstName,
                        u.Profile.LastName,
                        u.Profile.PhoneNumber,
                        u.Profile.Gender,
                        u.Profile.DateOfBirth
                    }
                })
                .FirstOrDefaultAsync();

            if (patient == null)
            {
                return NotFound(new
                {
                    message = "Patient not found."
                });
            }

            var appointments = await _context.Appointments
                .AsNoTracking()
                .Where(a => a.PatientId == id)
                .OrderByDescending(a => a.AppointmentDate)
                .Select(a => new
                {
                    a.Id,
                    a.AppointmentDate,
                    a.Status,
                    ServiceName = a.Service.Name,
                    DentistEmail = a.Doctor.Email
                })
                .ToListAsync();

            return Ok(new
            {
                Patient = patient,
                Appointments = appointments
            });
        }
    }
}
