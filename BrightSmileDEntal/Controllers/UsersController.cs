
using System.Security.Claims;
using BrightSmileDEntal.Constants;
using BrightSmileDEntal.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BrightSmileDEntal.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET /api/Users/me
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

        // GET /api/Users/patients
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

        // GET /api/Users/patients/{id}
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
