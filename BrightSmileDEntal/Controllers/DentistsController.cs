
using BrightSmileDEntal.Constants;
using BrightSmileDEntal.Data;
using BrightSmileDEntal.DTOs.Dentist;
using BrightSmileDEntal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BrightSmileDEntal.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DentistsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DentistsController> _logger;

        public DentistsController(
            ApplicationDbContext context,
            ILogger<DentistsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ==========================================
        // GET ALL DENTISTS - PUBLIC
        // GET /api/Dentists
        // ==========================================

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetDentists()
        {
            var dentists = await _context.Users
                .AsNoTracking()
                .Where(user =>
                    user.Role == UserRoles.Dentist)
                .Select(user => new
                {
                    Id = user.Id,

                    FirstName = user.Profile != null
                        ? user.Profile.FirstName
                        : string.Empty,

                    LastName = user.Profile != null
                        ? user.Profile.LastName
                        : string.Empty,

                    Email = user.Email
                })
                .OrderBy(dentist => dentist.FirstName)
                .ThenBy(dentist => dentist.LastName)
                .ToListAsync();

            var result = dentists.Select(dentist => new
            {
                dentist.Id,
                dentist.FirstName,
                dentist.LastName,
                dentist.Email,

                FullName = string.Join(
                    " ",
                    new[]
                    {
                        dentist.FirstName,
                        dentist.LastName
                    }.Where(name =>
                        !string.IsNullOrWhiteSpace(name))
                )
            });

            return Ok(result);
        }

        // ==========================================
        // GET DENTIST BY ID - PUBLIC
        // GET /api/Dentists/{id}
        // ==========================================

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetDentist(int id)
        {
            var dentist = await _context.Users
                .AsNoTracking()
                .Where(user =>
                    user.Id == id &&
                    user.Role == UserRoles.Dentist)
                .Select(user => new
                {
                    Id = user.Id,

                    FirstName = user.Profile != null
                        ? user.Profile.FirstName
                        : string.Empty,

                    LastName = user.Profile != null
                        ? user.Profile.LastName
                        : string.Empty,

                    Email = user.Email
                })
                .FirstOrDefaultAsync();

            if (dentist == null)
            {
                return NotFound(new
                {
                    message = "Dentist not found."
                });
            }

            var fullName = string.Join(
                " ",
                new[]
                {
                    dentist.FirstName,
                    dentist.LastName
                }.Where(name =>
                    !string.IsNullOrWhiteSpace(name))
            );

            return Ok(new
            {
                dentist.Id,
                dentist.FirstName,
                dentist.LastName,
                dentist.Email,
                FullName = fullName
            });
        }

        // ==========================================
        // GET ALL DENTISTS - ADMIN
        // GET /api/Dentists/admin
        // ==========================================

        [HttpGet("admin")]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> GetAdminDentists()
        {
            var now = DateTime.UtcNow;

            var dentists = await _context.Users
                .AsNoTracking()
                .Where(user =>
                    user.Role == UserRoles.Dentist)
                .OrderBy(user => user.Email)
                .Select(user => new
                {
                    user.Id,
                    user.Email,

                    FirstName = user.Profile != null
                        ? user.Profile.FirstName
                        : string.Empty,

                    LastName = user.Profile != null
                        ? user.Profile.LastName
                        : string.Empty,

                    PhoneNumber = user.Profile != null
                        ? user.Profile.PhoneNumber
                        : string.Empty,

                    AppointmentCount =
                        _context.Appointments.Count(
                            appointment =>
                                appointment.DoctorId == user.Id),

                    UpcomingAppointments =
                        _context.Appointments.Count(
                            appointment =>
                                appointment.DoctorId == user.Id &&
                                (
                                    appointment.Status == "Pending" ||
                                    appointment.Status == "Confirmed"
                                ) &&
                                appointment.AppointmentDate >= now)
                })
                .ToListAsync();

            return Ok(dentists);
        }

        // ==========================================
        // GET DENTIST DETAILS - ADMIN
        // GET /api/Dentists/admin/{id}
        // ==========================================

        [HttpGet("admin/{id:int}")]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> GetAdminDentist(int id)
        {
            var dentist = await _context.Users
                .AsNoTracking()
                .Where(user =>
                    user.Id == id &&
                    user.Role == UserRoles.Dentist)
                .Select(user => new
                {
                    user.Id,
                    user.Email,

                    Profile = user.Profile == null
                        ? null
                        : new
                        {
                            user.Profile.FirstName,
                            user.Profile.LastName,
                            user.Profile.PhoneNumber,
                            user.Profile.Gender
                        }
                })
                .FirstOrDefaultAsync();

            if (dentist == null)
            {
                return NotFound(new
                {
                    message = "Dentist not found."
                });
            }

            var appointments = await _context.Appointments
                .AsNoTracking()
                .Where(appointment =>
                    appointment.DoctorId == id)
                .OrderByDescending(appointment =>
                    appointment.AppointmentDate)
                .Select(appointment => new
                {
                    appointment.Id,
                    appointment.AppointmentDate,
                    appointment.Status,

                    ServiceName =
                        appointment.Service.Name,

                    PatientName =
                        appointment.Patient.Profile != null
                            ? appointment.Patient.Profile.FirstName
                                + " "
                                + appointment.Patient.Profile.LastName
                            : appointment.Patient.Email
                })
                .ToListAsync();

            return Ok(new
            {
                Dentist = dentist,
                Appointments = appointments
            });
        }

        // ==========================================
        // REGISTER NEW DENTIST - ADMIN
        // POST /api/Dentists/admin
        // ==========================================

        [HttpPost("admin")]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> CreateDentist(
            [FromBody] CreateDentistDto dto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var email = dto.Email.Trim().ToLowerInvariant();
            var firstName = dto.FirstName.Trim();
            var lastName = dto.LastName.Trim();
            var phoneNumber =
                dto.PhoneNumber?.Trim() ?? string.Empty;

            // ======================================
            // VALIDATE INPUT
            // ======================================

            if (firstName.Length < 2 ||
                lastName.Length < 2)
            {
                return BadRequest(new
                {
                    message =
                        "First name and last name must contain at least 2 characters."
                });
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new
                {
                    message = "Email address is required."
                });
            }

            // ======================================
            // CHECK EXISTING ACCOUNT
            // ======================================

            var emailExists = await _context.Users
                .AnyAsync(user =>
                    user.Email.ToLower() == email);

            if (emailExists)
            {
                return Conflict(new
                {
                    message =
                        "An account with this email address already exists."
                });
            }

            // ======================================
            // CREATE DENTIST ACCOUNT
            // ======================================

            var user = new User
            {
                Email = email,

                Role = UserRoles.Dentist,

                PasswordHash =
                    BCrypt.Net.BCrypt.HashPassword(
                        dto.Password)
            };

            // ======================================
            // CREATE DENTIST PROFILE
            // ======================================

            var profile = new Profile
            {
                User = user,
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = phoneNumber
            };

            _context.Profiles.Add(profile);

            // ======================================
            // SAVE TO DATABASE
            // ======================================

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
                when (ex.InnerException is PostgresException pgEx &&
                      pgEx.SqlState ==
                          PostgresErrorCodes.UniqueViolation)
            {
                _logger.LogWarning(
                    ex,
                    "Duplicate dentist account registration attempted.");

                return Conflict(new
                {
                    message =
                        "An account with this email address already exists."
                });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Database error while creating dentist account.");

                return StatusCode(500, new
                {
                    message =
                        "Unable to create dentist account due to a database error."
                });
            }

            _logger.LogInformation(
                "Dentist account created successfully. UserId: {UserId}",
                user.Id);

            return StatusCode(201, new
            {
                message =
                    "Dentist account created successfully.",
                user.Id,
                user.Email,
                user.Role
            });
        }
    }
}
