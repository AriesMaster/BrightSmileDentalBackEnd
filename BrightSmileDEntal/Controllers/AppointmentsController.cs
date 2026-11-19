
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

        private const int AppointmentDurationMinutes = 30;

        public AppointmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // -----------------------------------------------
        // CURRENT USER
        // -----------------------------------------------

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

        // -----------------------------------------------
        // VALIDATION HELPERS
        // -----------------------------------------------

        private static bool IsValidDate(DateTime date)
        {
            return date.Kind == DateTimeKind.Utc
                && date > DateTime.UtcNow
                && date <= DateTime.UtcNow.AddYears(2);
        }

        private static bool IsActive(string status)
        {
            return status == "Pending"
                || status == "Confirmed";
        }

        // -----------------------------------------------
        // POSTGRESQL BOOKING LOCK
        // Must be called inside an active transaction.
        // Serializes booking changes for each dentist.
        // -----------------------------------------------

        private async Task LockDentistSchedule(int doctorId)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(48291, {doctorId})");
        }

        // -----------------------------------------------
        // APPOINTMENT CONFLICT CHECK
        // Every appointment occupies 30 minutes.
        // -----------------------------------------------

        private async Task<bool> HasConflict(
            int doctorId,
            DateTime appointmentDate,
            int? excludeAppointmentId = null)
        {
            var appointmentEnd = appointmentDate
                .AddMinutes(AppointmentDurationMinutes);

            var earliestConflictingStart = appointmentDate
                .AddMinutes(-AppointmentDurationMinutes);

            return await _context.Appointments.AnyAsync(a =>
                a.DoctorId == doctorId
                && (a.Status == "Pending"
                    || a.Status == "Confirmed")
                && a.AppointmentDate > earliestConflictingStart
                && a.AppointmentDate < appointmentEnd
                && (!excludeAppointmentId.HasValue
                    || a.Id != excludeAppointmentId.Value));
        }

        // -----------------------------------------------
        // QUERY HELPERS
        // -----------------------------------------------

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

        // -----------------------------------------------
        // GET ALL APPOINTMENTS
        // ADMIN ONLY
        // -----------------------------------------------

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

        // -----------------------------------------------
        // GET PATIENT APPOINTMENTS
        // -----------------------------------------------

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

        // -----------------------------------------------
        // GET DENTIST APPOINTMENTS
        // -----------------------------------------------

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

        // -----------------------------------------------
        // GET APPOINTMENT BY ID
        // -----------------------------------------------

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
            {
                return NotFound(new
                {
                    message = "Appointment not found."
                });
            }

            if (!IsAdmin
                && appointment.PatientId != userId.Value
                && appointment.DoctorId != userId.Value)
            {
                return Forbid();
            }

            return Ok(MapToDto(appointment));
        }

        // -----------------------------------------------
        // CREATE APPOINTMENT
        // PATIENT ONLY
        // -----------------------------------------------

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
                        "Appointment date must be a future UTC date within two years."
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

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            // Prevent simultaneous bookings for this dentist.
            await LockDentistSchedule(dto.DoctorId);

            if (await HasConflict(
                dto.DoctorId,
                dto.AppointmentDate))
            {
                return Conflict(new
                {
                    message =
                        "The dentist already has an appointment during this time."
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
            await transaction.CommitAsync();

            var created = await AppointmentQuery()
                .FirstAsync(a => a.Id == appointment.Id);

            return CreatedAtAction(
                nameof(GetAppointment),
                new { id = appointment.Id },
                MapToDto(created));
        }

        // -----------------------------------------------
        // RESCHEDULE APPOINTMENT
        // PATIENT ONLY
        // -----------------------------------------------

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

            if (!IsValidDate(dto.AppointmentDate))
            {
                return BadRequest(new
                {
                    message =
                        "Appointment date must be a future UTC date within two years."
                });
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            // Read the dentist ID before acquiring the lock.
            var doctorId = await _context.Appointments
                .Where(a => a.Id == id)
                .Select(a => (int?)a.DoctorId)
                .FirstOrDefaultAsync();

            if (doctorId == null)
            {
                return NotFound(new
                {
                    message = "Appointment not found."
                });
            }

            await LockDentistSchedule(doctorId.Value);

            // Reload after locking to obtain the latest status.
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
            {
                return NotFound(new
                {
                    message = "Appointment not found."
                });
            }

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

            if (await HasConflict(
                appointment.DoctorId,
                dto.AppointmentDate,
                appointment.Id))
            {
                return Conflict(new
                {
                    message =
                        "The dentist already has an appointment during this time."
                });
            }

            appointment.AppointmentDate = dto.AppointmentDate;
            appointment.AdditionalInfo =
                dto.AdditionalInfo?.Trim();

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var updated = await AppointmentQuery()
                .FirstAsync(a => a.Id == id);

            return Ok(MapToDto(updated));
        }

        // -----------------------------------------------
        // UPDATE APPOINTMENT STATUS
        // DENTIST OR ADMIN
        // -----------------------------------------------

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

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            var doctorId = await _context.Appointments
                .Where(a => a.Id == id)
                .Select(a => (int?)a.DoctorId)
                .FirstOrDefaultAsync();

            if (doctorId == null)
            {
                return NotFound(new
                {
                    message = "Appointment not found."
                });
            }

            await LockDentistSchedule(doctorId.Value);

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
            {
                return NotFound(new
                {
                    message = "Appointment not found."
                });
            }

            if (!IsAdmin
                && appointment.DoctorId != userId.Value)
            {
                return Forbid();
            }

            if (!IsActive(appointment.Status))
            {
                return Conflict(new
                {
                    message =
                        "This appointment can no longer be updated."
                });
            }

            // Only pending appointments can be confirmed.
            if (status == "Confirmed"
                && appointment.Status != "Pending")
            {
                return Conflict(new
                {
                    message =
                        "Only pending appointments can be confirmed."
                });
            }

            // Require confirmation before completion.
            if (status == "Completed"
                && appointment.Status != "Confirmed")
            {
                return Conflict(new
                {
                    message =
                        "Only confirmed appointments can be completed."
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
            await transaction.CommitAsync();

            return Ok(new
            {
                message = "Appointment status updated.",
                appointment.Id,
                appointment.Status
            });
        }

        // -----------------------------------------------
        // CANCEL APPOINTMENT
        // PATIENT OR ADMIN
        // -----------------------------------------------

        [HttpPatch("{id:int}/cancel")]
        [Authorize(Roles =
            UserRoles.Patient + "," + UserRoles.Admin)]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            var userId = CurrentUserId;

            if (userId == null)
                return Unauthorized();

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            var doctorId = await _context.Appointments
                .Where(a => a.Id == id)
                .Select(a => (int?)a.DoctorId)
                .FirstOrDefaultAsync();

            if (doctorId == null)
            {
                return NotFound(new
                {
                    message = "Appointment not found."
                });
            }

            await LockDentistSchedule(doctorId.Value);

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
            {
                return NotFound(new
                {
                    message = "Appointment not found."
                });
            }

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
            await transaction.CommitAsync();

            return Ok(new
            {
                message = "Appointment cancelled successfully.",
                appointment.Id,
                appointment.Status
            });
        }
    }
}
