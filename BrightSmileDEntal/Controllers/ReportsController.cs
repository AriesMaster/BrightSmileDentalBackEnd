
using BrightSmileDEntal.Constants;
using BrightSmileDEntal.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BrightSmileDEntal.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.Admin)]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private static readonly TimeSpan SastOffset = TimeSpan.FromHours(2);

    public ReportsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET /api/Reports/overview?from=2026-11-01&to=2026-11-30
    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(
            DateTime.UtcNow.Add(SastOffset));

        var startDate = from ?? today.AddDays(-29);
        var endDate = to ?? today;

        if (startDate > endDate)
        {
            return BadRequest(new
            {
                message = "The start date cannot be after the end date."
            });
        }

        if (endDate.DayNumber - startDate.DayNumber > 365)
        {
            return BadRequest(new
            {
                message = "The reporting range cannot exceed 366 days."
            });
        }

        // Interpret date filters as South African calendar days.
        var startUtc = DateTime.SpecifyKind(
            startDate.ToDateTime(TimeOnly.MinValue)
                .Subtract(SastOffset),
            DateTimeKind.Utc);

        var endUtc = DateTime.SpecifyKind(
            endDate.AddDays(1).ToDateTime(TimeOnly.MinValue)
                .Subtract(SastOffset),
            DateTimeKind.Utc);

        var nowUtc = DateTime.UtcNow;

        // Fetch only the fields needed for reporting.
        var appointments = await _context.Appointments
            .AsNoTracking()
            .Where(a =>
                a.AppointmentDate >= startUtc &&
                a.AppointmentDate < endUtc)
            .Select(a => new
            {
                a.Id,
                a.PatientId,
                a.DoctorId,
                a.AppointmentDate,
                a.Status,
                ServiceName = a.Service.Name,
                ServicePrice = a.Service.Price,
                DentistEmail = a.Doctor.Email,
                DentistFirstName = a.Doctor.Profile != null
                    ? a.Doctor.Profile.FirstName
                    : "",
                DentistLastName = a.Doctor.Profile != null
                    ? a.Doctor.Profile.LastName
                    : ""
            })
            .ToListAsync(cancellationToken);

        var total = appointments.Count;

        var pending = appointments.Count(a =>
            a.Status == "Pending");

        var confirmed = appointments.Count(a =>
            a.Status == "Confirmed");

        var completed = appointments.Count(a =>
            a.Status == "Completed");

        var cancelled = appointments.Count(a =>
            a.Status == "Cancelled");

        var other = total - pending - confirmed -
                    completed - cancelled;

        // Estimated value only, not collected revenue.
        var completedValue = appointments
            .Where(a => a.Status == "Completed")
            .Sum(a => a.ServicePrice);

        var scheduledValue = appointments
            .Where(a => a.Status == "Pending" ||
                        a.Status == "Confirmed")
            .Sum(a => a.ServicePrice);

        var dailyCounts = appointments
            .GroupBy(a => DateOnly.FromDateTime(
                a.AppointmentDate.Add(SastOffset)))
            .ToDictionary(g => g.Key, g => g.Count());

        // Include dates with zero appointments.
        var dailyTrend = Enumerable
            .Range(0, endDate.DayNumber - startDate.DayNumber + 1)
            .Select(offset =>
            {
                var date = startDate.AddDays(offset);

                return new
                {
                    date = date.ToString("yyyy-MM-dd"),
                    count = dailyCounts.GetValueOrDefault(date)
                };
            })
            .ToList();

        var statusBreakdown = new[]
        {
            new { status = "Pending", count = pending },
            new { status = "Confirmed", count = confirmed },
            new { status = "Completed", count = completed },
            new { status = "Cancelled", count = cancelled },
            new { status = "Other", count = other }
        };

        var popularServices = appointments
            .GroupBy(a => a.ServiceName)
            .Select(g => new
            {
                serviceName = g.Key,
                appointmentCount = g.Count(),
                completedCount = g.Count(a =>
                    a.Status == "Completed"),
                estimatedCompletedValue = g
                    .Where(a => a.Status == "Completed")
                    .Sum(a => a.ServicePrice)
            })
            .OrderByDescending(x => x.appointmentCount)
            .ThenBy(x => x.serviceName)
            .ToList();

        var dentistWorkload = appointments
            .GroupBy(a => new
            {
                a.DoctorId,
                a.DentistEmail,
                a.DentistFirstName,
                a.DentistLastName
            })
            .Select(g => new
            {
                dentistId = g.Key.DoctorId,
                dentistName = string.IsNullOrWhiteSpace(
                    $"{g.Key.DentistFirstName} {g.Key.DentistLastName}")
                    ? g.Key.DentistEmail
                    : $"{g.Key.DentistFirstName} {g.Key.DentistLastName}".Trim(),
                appointmentCount = g.Count(),
                pendingCount = g.Count(a =>
                    a.Status == "Pending"),
                confirmedCount = g.Count(a =>
                    a.Status == "Confirmed"),
                completedCount = g.Count(a =>
                    a.Status == "Completed"),
                cancelledCount = g.Count(a =>
                    a.Status == "Cancelled")
            })
            .OrderByDescending(x => x.appointmentCount)
            .ToList();

        return Ok(new
        {
            period = new
            {
                from = startDate.ToString("yyyy-MM-dd"),
                to = endDate.ToString("yyyy-MM-dd"),
                timezone = "Africa/Johannesburg"
            },
            summary = new
            {
                totalAppointments = total,
                pendingAppointments = pending,
                confirmedAppointments = confirmed,
                completedAppointments = completed,
                cancelledAppointments = cancelled,
                otherAppointments = other,
                uniquePatients = appointments
                    .Select(a => a.PatientId)
                    .Distinct()
                    .Count(),
                upcomingAppointments = appointments.Count(a =>
                    a.AppointmentDate > nowUtc &&
                    (a.Status == "Pending" ||
                     a.Status == "Confirmed")),
                completionRate = total == 0
                    ? 0m
                    : Math.Round(completed * 100m / total, 1),
                cancellationRate = total == 0
                    ? 0m
                    : Math.Round(cancelled * 100m / total, 1),
                estimatedCompletedValue = completedValue,
                estimatedScheduledValue = scheduledValue,
                currency = "ZAR"
            },
            statusBreakdown,
            dailyTrend,
            popularServices,
            dentistWorkload
        });
    }
}
