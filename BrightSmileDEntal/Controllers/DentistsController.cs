
using BrightSmileDEntal.Constants;
using BrightSmileDEntal.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BrightSmileDEntal.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DentistsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DentistsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // GET ALL DENTISTS
        // GET /api/Dentists
        // ==========================================

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetDentists()
        {
            var dentists = await _context.Users
                .AsNoTracking()
                .Where(user =>
                    user.Role == UserRoles.Dentist
                )
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
                        !string.IsNullOrWhiteSpace(name)
                    )
                )
            });

            return Ok(result);
        }

        // ==========================================
        // GET DENTIST BY ID
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
                    user.Role == UserRoles.Dentist
                )
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
                    !string.IsNullOrWhiteSpace(name)
                )
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
    }
}
