
using System.Security.Claims;
using BrightSmileDEntal.Constants;
using BrightSmileDEntal.Data;
using BrightSmileDEntal.DTOs.Appointment;
using BrightSmileDEntal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BrightSmileDEntal.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class AppointmentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AppointmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int? CurrentUserId
        {
            get
            {
                var value = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                return int.TryParse(value, out var id)
                    ? id
                    : null;
            }
        }

        private bool IsAdmin =>
            User.IsInRole(UserRoles.Admin);

        private static bool IsValidDate(DateTime date)
        {
            return date.Kind == DateTimeKind.Utc
                && date > DateTime.UtcNow;
        }

        private static bool IsActive(string status)
        {
            return status == "Pending"
                || status == "Confirmed";
        }

        private async Task<bool> HasConflict(
            int doctorId,
            DateTime appointmentDate,
            int? excludeAppointmentId = null)
        {
            return await _context.Appointments.AnyAsync(a =>
                a.DoctorId == doctorId
                && a.AppointmentDate == appointmentDate
                && (a.Status == "Pending"
                    || a.Status == "Confirmed")
                && (!excludeAppointmentId.HasValue
                    || a.Id != excludeAppointmentId.Value));
        }

        private IQueryable<Appointment> AppointmentQuery()
        {
            return _context.Appointments
                .AsNoTracking()
                .Include(a => a.Patient)
                    .ThenInclude(p => p.Profile)
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.Profile)
                .Include(a => a.Service);
        }

        private static string GetDisplayName(User user)
        {
            if (user.Profile == null)
                return user.Email;

            var name = string.Join(" ",
                new[]
                {
                    user.Profile.FirstName,
                    user.Profile.LastName
                }.Where(n => !string.IsNullOrWhiteSpace(n)));

            return string.IsNullOrWhiteSpace(name)
                ? user.Email
                : name;
        }

        private static AppointmentDto MapToDto(
            Appointment appointment)
        {
            return new AppointmentDto
            {
                Id = appointment.Id,
                PatientId = appointment.PatientId,
                PatientName = GetDisplayName(
                    appointment.Patient),
                DoctorId = appointment.DoctorId,
                DoctorName = GetDisplayName(
                    appointment.Doctor),
                ServiceId = appointment.ServiceId,
                ServiceName = appointment.Service.Name,
                ServicePrice = appointment.Service.Price,
                AppointmentDate = appointment.AppointmentDate,
                Status = appointment.Status,
                AdditionalInfo = appointment.AdditionalInfo
            };
        }

        // GET: api/Appointments
        // Admin only
        [HttpGet]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<ActionResult<IEnumerable<AppointmentDto>>>
            GetAllAppointments()
        {
            var appointments = await AppointmentQuery()
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync();

            return Ok(appointments.Select(MapToDto));
        }

        // GET: api/Appointments/me
        // Patient only
        [HttpGet("me")]
        [Authorize(Roles = UserRoles.Patient)]
        public async Task<ActionResult<IEnumerable<AppointmentDto>>>
            GetMyAppointments()
        {
            var userId = CurrentUserId;

            if (userId == null)
                return Unauthorized();

            var appointments = await AppointmentQuery()
                .Where(a => a.PatientId == userId.Value)
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync();

            return Ok(appointments.Select(MapToDto));
        }

        // GET: api/Appointments/dentist/me
        // Dentist only
        [HttpGet("dentist/me")]
        [Authorize(Roles = UserRoles.Dentist)]
        public async Task<ActionResult<IEnumerable<AppointmentDto>>>
            GetDentistAppointments()
        {
            var userId = CurrentUserId;

            if (userId == null)
                return Unauthorized();

            var appointments = await AppointmentQuery()
                .Where(a => a.DoctorId == userId.Value)
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync();

            return Ok(appointments.Select(MapToDto));
        }

        // GET: api/Appointments/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<AppointmentDto>>
            GetAppointment(int id)
        {
            var userId = CurrentUserId;

            if (userId == null)
                return Unauthorized();

            var appointment = await AppointmentQuery()
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
                return NotFound(new
                {
                    message = "Appointment not found."
                });

            if (!IsAdmin
                && appointment.PatientId != userId.Value
                && appointment.DoctorId != userId.Value)
            {
                return Forbid();
            }

            return Ok(MapToDto(appointment));
        }

        // POST: api/Appointments
        [HttpPost]
        [Authorize(Roles = UserRoles.Patient)]
        public async Task<ActionResult<AppointmentDto>>
            CreateAppointment(
                [FromBody] CreateAppointmentDto dto)
        {
            var patientId = CurrentUserId;

            if (patientId == null)
                return Unauthorized();

            if (!IsValidDate(dto.AppointmentDate))
            {
                return BadRequest(new
                {
                    message =
                        "Appointment date must be a future UTC date."
                });
            }

            var doctorExists = await _context.Users
                .AnyAsync(u =>
                    u.Id == dto.DoctorId
                    && u.Role == UserRoles.Dentist);

            if (!doctorExists)
            {
                return BadRequest(new
                {
                    message = "Selected dentist is invalid."
                });
            }

            var serviceExists = await _context.Services
                .AnyAsync(s => s.Id == dto.ServiceId);

            if (!serviceExists)
            {
                return BadRequest(new
                {
                    message = "Selected service is invalid."
                });
            }

            if (await HasConflict(
                dto.DoctorId,
                dto.AppointmentDate))
            {
                return Conflict(new
                {
                    message =
                        "The dentist already has an appointment at this time."
                });
            }

            var appointment = new Appointment
            {
                PatientId = patientId.Value,
                DoctorId = dto.DoctorId,
                ServiceId = dto.ServiceId,
                AppointmentDate = dto.AppointmentDate,
                AdditionalInfo = dto.AdditionalInfo?.Trim(),
                Status = "Pending"
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            var created = await AppointmentQuery()
                .FirstAsync(a => a.Id == appointment.Id);

            return CreatedAtAction(
                nameof(GetAppointment),
                new { id = appointment.Id },
                MapToDto(created));
        }

        // PUT: api/Appointments/5
        // Patient reschedules their own appointment
        [HttpPut("{id:int}")]
        [Authorize(Roles = UserRoles.Patient)]
        public async Task<ActionResult<AppointmentDto>>
            UpdateAppointment(
                int id,
                [FromBody] UpdateAppointmentDto dto)
        {
            var patientId = CurrentUserId;

            if (patientId == null)
                return Unauthorized();

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
                return NotFound(new
                {
                    message = "Appointment not found."
                });

            if (appointment.PatientId != patientId.Value)
                return Forbid();

            if (appointment.Status != "Pending")
            {
                return Conflict(new
                {
                    message =
                        "Only pending appointments can be rescheduled."
                });
            }

            if (!IsValidDate(dto.AppointmentDate))
            {
                return BadRequest(new
                {
                    message =
                        "Appointment date must be a future UTC date."
                });
            }

            if (await HasConflict(
                appointment.DoctorId,
                dto.AppointmentDate,
                appointment.Id))
            {
                return Conflict(new
                {
                    message =
                        "The dentist already has an appointment at this time."
                });
            }

            appointment.AppointmentDate = dto.AppointmentDate;
            appointment.AdditionalInfo =
                dto.AdditionalInfo?.Trim();

            await _context.SaveChangesAsync();

            var updated = await AppointmentQuery()
                .FirstAsync(a => a.Id == id);

            return Ok(MapToDto(updated));
        }

        // PATCH: api/Appointments/5/status
        // Dentist or Admin
        [HttpPatch("{id:int}/status")]
        [Authorize(Roles =
            UserRoles.Dentist + "," + UserRoles.Admin)]
        public async Task<IActionResult> UpdateStatus(
            int id,
            [FromBody] UpdateAppointmentStatusDto dto)
        {
            var userId = CurrentUserId;

            if (userId == null)
                return Unauthorized();

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
                return NotFound(new
                {
                    message = "Appointment not found."
                });

            if (!IsAdmin
                && appointment.DoctorId != userId.Value)
            {
                return Forbid();
            }

            var status = dto.Status?.Trim();

            if (status != "Confirmed"
                && status != "Completed"
                && status != "Cancelled")
            {
                return BadRequest(new
                {
                    message =
                        "Status must be Confirmed, Completed or Cancelled."
                });
            }

            if (!IsActive(appointment.Status))
            {
                return Conflict(new
                {
                    message =
                        "This appointment can no longer be updated."
                });
            }

            if (status == "Completed"
                && appointment.AppointmentDate > DateTime.UtcNow)
            {
                return Conflict(new
                {
                    message =
                        "A future appointment cannot be completed."
                });
            }

            appointment.Status = status;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Appointment status updated.",
                appointment.Id,
                appointment.Status
            });
        }

        // PATCH: api/Appointments/5/cancel
        // Patient or Admin
        [HttpPatch("{id:int}/cancel")]
        [Authorize(Roles =
            UserRoles.Patient + "," + UserRoles.Admin)]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            var userId = CurrentUserId;

            if (userId == null)
                return Unauthorized();

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
                return NotFound(new
                {
                    message = "Appointment not found."
                });

            if (!IsAdmin
                && appointment.PatientId != userId.Value)
            {
                return Forbid();
            }

            if (!IsActive(appointment.Status))
            {
                return Conflict(new
                {
                    message =
                        "Only active appointments can be cancelled."
                });
            }

            appointment.Status = "Cancelled";
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Appointment cancelled successfully.",
                appointment.Id,
                appointment.Status
            });
        }
    }
}
