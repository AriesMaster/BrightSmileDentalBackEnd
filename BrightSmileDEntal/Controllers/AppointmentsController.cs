
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
    /// <summary>
    /// Manages appointment bookings, availability, and status changes.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class AppointmentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        private const int AppointmentDurationMinutes = 30;

        private static readonly TimeSpan OpeningTime =
            new(9, 0, 0);

        private static readonly TimeSpan ClosingTime =
            new(17, 0, 0);

        private static readonly TimeSpan SouthAfricaUtcOffset =
            TimeSpan.FromHours(2);

        /// <summary>
        /// Initializes the appointment controller.
        /// </summary>
        /// <param name="context">The database context used to access appointments.</param>
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
                && date > DateTime.UtcNow
                && date <= DateTime.UtcNow.AddYears(2);
        }

        private static bool IsWithinOperatingHours(
            DateTime appointmentDate)
        {
            if (appointmentDate.Kind != DateTimeKind.Utc)
                return false;

            // South Africa uses UTC+02:00 year-round.
            var southAfricanTime =
                appointmentDate.Add(SouthAfricaUtcOffset);

            // Monday to Friday only.
            if (southAfricanTime.DayOfWeek ==
                    DayOfWeek.Saturday ||
                southAfricanTime.DayOfWeek ==
                    DayOfWeek.Sunday)
            {
                return false;
            }

            // Only :00 and :30 appointment starts.
            if (southAfricanTime.Minute != 0 &&
                southAfricanTime.Minute != 30)
            {
                return false;
            }

            // No extra seconds or fractional seconds.
            if (southAfricanTime.Second != 0 ||
                southAfricanTime.Ticks %
                    TimeSpan.TicksPerSecond != 0)
            {
                return false;
            }

            var appointmentStart =
                southAfricanTime.TimeOfDay;

            var appointmentEnd =
                appointmentStart.Add(
                    TimeSpan.FromMinutes(
                        AppointmentDurationMinutes));

            return appointmentStart >= OpeningTime &&
                   appointmentEnd <= ClosingTime;
        }

        private static bool IsActive(string status)
        {
            return status == "Pending"
                || status == "Confirmed";
        }


        // The transaction-scoped lock prevents concurrent bookings for the same dentist.
        private async Task LockDentistSchedule(int doctorId)
        {
            // Must be called within an active transaction.
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(48291, {doctorId})");
        }


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
                && a.AppointmentDate >
                    earliestConflictingStart
                && a.AppointmentDate < appointmentEnd
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


        /// <summary>
        /// Returns all appointments, ordered by appointment date.
        /// </summary>
        /// <remarks>
        /// Accessible to administrators only.
        /// </remarks>
        /// <response code="200">Request completed successfully.</response>
        /// <response code="401">Authentication is required.</response>
        /// <response code="403">Access is forbidden.</response>
        [HttpGet]
        [Authorize(Roles = UserRoles.Admin)]
        [ProducesResponseType(typeof(IEnumerable<AppointmentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(IEnumerable<AppointmentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(IEnumerable<AppointmentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<AppointmentDto>>>
            GetAllAppointments()
        {
            var appointments = await AppointmentQuery()
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync();

            return Ok(appointments.Select(MapToDto));
        }


        /// <summary>
        /// Returns appointments belonging to the authenticated patient.
        /// </summary>
        /// <remarks>
        /// Accessible to patients only.
        /// </remarks>
        /// <response code="200">Request completed successfully.</response>
        /// <response code="401">Authentication is required.</response>
        /// <response code="403">Access is forbidden.</response>
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


        /// <summary>
        /// Returns appointments assigned to the authenticated dentist.
        /// </summary>
        /// <remarks>
        /// Accessible to dentists only.
        /// </remarks>
        /// <response code="200">Request completed successfully.</response>
        /// <response code="401">Authentication is required.</response>
        /// <response code="403">Access is forbidden.</response>
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


        /// <summary>
        /// Returns an appointment by its identifier.
        /// </summary>
        /// <remarks>
        /// Accessible to the assigned patient, dentist, or an administrator.
        /// </remarks>
        /// <response code="200">Request completed successfully.</response>
        /// <response code="401">Authentication is required.</response>
        /// <response code="403">Access is forbidden.</response>
        /// <response code="404">The requested resource was not found.</response>
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


        /// <summary>
        /// Returns available appointment start times for a dentist on a specified date.
        /// </summary>
        /// <remarks>
        /// Times are returned in South African Standard Time (SAST). The practice operates Monday to Friday, 09:00–17:00, in 30-minute slots. Past and occupied slots are excluded.
        /// </remarks>
        /// <response code="200">Request completed successfully.</response>
        /// <response code="400">The request is invalid.</response>
        /// <response code="401">Authentication is required.</response>
        /// <response code="403">Access is forbidden.</response>
        /// <response code="404">The requested resource was not found.</response>
        [HttpGet("availability")]
        [Authorize(Roles = UserRoles.Patient)]
        public async Task<IActionResult> GetAvailableSlots(
            [FromQuery] int doctorId,
            [FromQuery] DateOnly date)
        {
            if (doctorId <= 0)
            {
                return BadRequest(new
                {
                    message = "Please select a valid dentist."
                });
            }

            var doctorExists = await _context.Users
                .AnyAsync(u =>
                    u.Id == doctorId &&
                    u.Role == UserRoles.Dentist);

            if (!doctorExists)
            {
                return NotFound(new
                {
                    message = "Dentist not found."
                });
            }

            var requestedDate =
                date.ToDateTime(TimeOnly.MinValue);

            var todaySast = DateTime.UtcNow
                .Add(SouthAfricaUtcOffset)
                .Date;

            if (requestedDate < todaySast ||
                requestedDate > todaySast.AddYears(2))
            {
                return BadRequest(new
                {
                    message =
                        "Select a date within the next two years."
                });
            }

            // The dental practice is closed on weekends.
            if (date.DayOfWeek == DayOfWeek.Saturday ||
                date.DayOfWeek == DayOfWeek.Sunday)
            {
                return Ok(Array.Empty<string>());
            }

            // Convert the beginning of the SAST day to UTC.
            var dayStartUtc = DateTime.SpecifyKind(
                requestedDate.Subtract(
                    SouthAfricaUtcOffset),
                DateTimeKind.Utc);

            var dayEndUtc = dayStartUtc.AddDays(1);

            // Fetch active appointments for the dentist.
            // Include bookings overlapping the day boundary.
            var existingBookings = await _context.Appointments
                .AsNoTracking()
                .Where(a =>
                    a.DoctorId == doctorId &&
                    (a.Status == "Pending" ||
                     a.Status == "Confirmed") &&
                    a.AppointmentDate >
                        dayStartUtc.AddMinutes(
                            -AppointmentDurationMinutes) &&
                    a.AppointmentDate < dayEndUtc)
                .Select(a => a.AppointmentDate)
                .ToListAsync();

            var availableSlots = new List<string>();
            var nowUtc = DateTime.UtcNow;

            // Generate 09:00, 09:30 ... 16:30.
            for (
                var time = OpeningTime;
                time.Add(TimeSpan.FromMinutes(
                    AppointmentDurationMinutes)) <= ClosingTime;
                time = time.Add(TimeSpan.FromMinutes(
                    AppointmentDurationMinutes))
            )
            {
                // Convert SAST slot to UTC.
                var slotStartUtc = DateTime.SpecifyKind(
                    requestedDate
                        .Add(time)
                        .Subtract(SouthAfricaUtcOffset),
                    DateTimeKind.Utc);

                // Do not return past slots.
                if (slotStartUtc <= nowUtc)
                    continue;

                var slotEndUtc = slotStartUtc.AddMinutes(
                    AppointmentDurationMinutes);

                // Check for overlap with an existing booking.
                var hasConflict = existingBookings.Any(
                    bookedStart =>
                        bookedStart < slotEndUtc &&
                        bookedStart.AddMinutes(
                            AppointmentDurationMinutes) >
                            slotStartUtc);

                if (!hasConflict)
                {
                    availableSlots.Add(
                        time.ToString(@"hh\:mm"));
                }
            }

            return Ok(availableSlots);
        }


        /// <summary>
        /// Creates an appointment for the authenticated patient.
        /// </summary>
        /// <remarks>
        /// The requested UTC date must be within two years and correspond to an available 30-minute slot during operating hours. A transaction-level PostgreSQL advisory lock serializes bookings for the same dentist.
        /// </remarks>
        /// <response code="201">Appointment created.</response>
        /// <response code="400">The request is invalid.</response>
        /// <response code="401">Authentication is required.</response>
        /// <response code="403">Access is forbidden.</response>
        /// <response code="409">The requested operation conflicts with the current appointment state.</response>
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

            if (!IsWithinOperatingHours(dto.AppointmentDate))
            {
                return BadRequest(new
                {
                    message =
                        "Appointments are available Monday to Friday, " +
                        "09:00 to 17:00 South African time. " +
                        "Please select a 30-minute slot between " +
                        "09:00 and 16:30."
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

            // Serialize bookings for this dentist.
            await LockDentistSchedule(dto.DoctorId);

            // Recheck availability inside the transaction.
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


        /// <summary>
        /// Reschedules a pending appointment owned by the authenticated patient.
        /// </summary>
        /// <remarks>
        /// The new UTC date must meet operating-hour and availability requirements. Concurrent changes to the dentist's schedule are serialized within a database transaction.
        /// </remarks>
        /// <response code="200">Request completed successfully.</response>
        /// <response code="400">The request is invalid.</response>
        /// <response code="401">Authentication is required.</response>
        /// <response code="403">Access is forbidden.</response>
        /// <response code="404">The requested resource was not found.</response>
        /// <response code="409">The requested operation conflicts with the current appointment state.</response>
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

            if (!IsWithinOperatingHours(dto.AppointmentDate))
            {
                return BadRequest(new
                {
                    message =
                        "Appointments are available Monday to Friday, " +
                        "09:00 to 17:00 South African time. " +
                        "Please select a 30-minute slot between " +
                        "09:00 and 16:30."
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


        /// <summary>
        /// Updates an appointment's status.
        /// </summary>
        /// <remarks>
        /// Dentists may update their own appointments; administrators may update any appointment. Pending appointments can be confirmed, and confirmed appointments can be completed once their start time has passed. Active appointments may be cancelled.
        /// </remarks>
        /// <response code="200">Request completed successfully.</response>
        /// <response code="400">The request is invalid.</response>
        /// <response code="401">Authentication is required.</response>
        /// <response code="403">Access is forbidden.</response>
        /// <response code="404">The requested resource was not found.</response>
        /// <response code="409">The requested operation conflicts with the current appointment state.</response>
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

            if (status == "Confirmed"
                && appointment.Status != "Pending")
            {
                return Conflict(new
                {
                    message =
                        "Only pending appointments can be confirmed."
                });
            }

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


        /// <summary>
        /// Cancels an active appointment.
        /// </summary>
        /// <remarks>
        /// Patients may cancel their own appointments; administrators may cancel any active appointment.
        /// </remarks>
        /// <response code="200">Request completed successfully.</response>
        /// <response code="401">Authentication is required.</response>
        /// <response code="403">Access is forbidden.</response>
        /// <response code="404">The requested resource was not found.</response>
        /// <response code="409">The requested operation conflicts with the current appointment state.</response>
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
