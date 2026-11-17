
using BrightSmileDEntal.Constants;
using BrightSmileDEntal.Data;
using BrightSmileDEntal.DTOs.Service;
using BrightSmileDEntal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BrightSmileDEntal.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServicesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ServicesController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Returns all available dental services.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ServiceDto>>> GetServices()
        {
            var services = await _context.Services
                .AsNoTracking()
                .OrderBy(s => s.Name)
                .Select(s => new ServiceDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    Price = s.Price
                })
                .ToListAsync();

            return Ok(services);
        }

        /// <summary>
        /// Returns a dental service by its ID.
        /// </summary>
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<ServiceDto>> GetService(int id)
        {
            var service = await _context.Services
                .AsNoTracking()
                .Where(s => s.Id == id)
                .Select(s => new ServiceDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    Price = s.Price
                })
                .FirstOrDefaultAsync();

            if (service == null)
            {
                return NotFound(new
                {
                    message = "Dental service not found."
                });
            }

            return Ok(service);
        }

        /// <summary>
        /// Creates a new dental service. Admin access is required.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<ActionResult<ServiceDto>> CreateService(
            [FromBody] CreateServiceDto dto)
        {
            var name = dto.Name?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(new
                {
                    message = "Service name is required."
                });
            }

            var normalizedName = name.ToLowerInvariant();

            var nameExists = await _context.Services
                .AnyAsync(s => s.Name.ToLower() == normalizedName);

            if (nameExists)
            {
                return Conflict(new
                {
                    message = "A dental service with this name already exists."
                });
            }

            var service = new Service
            {
                Name = name,
                Description = dto.Description?.Trim() ?? string.Empty,
                Price = dto.Price
            };

            _context.Services.Add(service);
            await _context.SaveChangesAsync();

            var result = MapToDto(service);

            return CreatedAtAction(
                nameof(GetService),
                new { id = service.Id },
                result
            );
        }

        /// <summary>
        /// Updates an existing dental service. Admin access is required.
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<ActionResult<ServiceDto>> UpdateService(
            int id,
            [FromBody] UpdateServiceDto dto)
        {
            var service = await _context.Services
                .FirstOrDefaultAsync(s => s.Id == id);

            if (service == null)
            {
                return NotFound(new
                {
                    message = "Dental service not found."
                });
            }

            var name = dto.Name?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(new
                {
                    message = "Service name is required."
                });
            }

            var normalizedName = name.ToLowerInvariant();

            // Exclude the current service when checking name uniqueness.
            var duplicateExists = await _context.Services
                .AnyAsync(s =>
                    s.Id != id &&
                    s.Name.ToLower() == normalizedName);

            if (duplicateExists)
            {
                return Conflict(new
                {
                    message = "Another dental service already uses this name."
                });
            }

            service.Name = name;
            service.Description = dto.Description?.Trim() ?? string.Empty;
            service.Price = dto.Price;

            await _context.SaveChangesAsync();

            return Ok(MapToDto(service));
        }

        /// <summary>
        /// Deletes a service that has no associated appointments.
        /// </summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> DeleteService(int id)
        {
            var service = await _context.Services
                .FirstOrDefaultAsync(s => s.Id == id);

            if (service == null)
            {
                return NotFound(new
                {
                    message = "Dental service not found."
                });
            }

            // Preserve appointment history by blocking deletion of used services.
            var hasAppointments = await _context.Appointments
                .AnyAsync(a => a.ServiceId == id);

            if (hasAppointments)
            {
                return Conflict(new
                {
                    message = "This service cannot be deleted because it is associated with existing appointments."
                });
            }

            _context.Services.Remove(service);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private static ServiceDto MapToDto(Service service)
        {
            return new ServiceDto
            {
                Id = service.Id,
                Name = service.Name,
                Description = service.Description,
                Price = service.Price
            };
        }
    }
}
