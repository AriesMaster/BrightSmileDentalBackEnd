using BrightSmileDEntal.Constants;
using BrightSmileDEntal.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightSmileDEntal.Data
{
    public static class DbSeeder
    {
        public static async Task SeedDemoUsersAsync(ApplicationDbContext context, ILogger logger)
        {
            logger.LogInformation("Demo user seeding started.");

            var demoUsers = new[]
            {
                new { Email = "admin@brightsmile.com", Password = "Demo@1234", Role = UserRoles.Admin },
                new { Email = "dentist@brightsmile.com", Password = "Demo@1234", Role = UserRoles.Dentist },
                new { Email = "patient@brightsmile.com", Password = "Demo@1234", Role = UserRoles.Patient }
            };

            var anyChanged = false;

            foreach (var demo in demoUsers)
            {
                var user = await context.Users.SingleOrDefaultAsync(u => u.Email == demo.Email);

                if (user is null)
                {
                    context.Users.Add(new User
                    {
                        Email = demo.Email,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(demo.Password),
                        Role = demo.Role
                    });

                    anyChanged = true;
                    logger.LogInformation("Added demo user {Email} with role {Role}.", demo.Email, demo.Role);
                    continue;
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(demo.Password);
                user.Role = demo.Role;
                anyChanged = true;
                logger.LogInformation("Refreshed credentials for demo user {Email}.", demo.Email);
            }

            if (anyChanged)
            {
                await context.SaveChangesAsync();
                logger.LogInformation("Demo users saved successfully.");
            }

            logger.LogInformation("Demo user seeding finished.");
        }
    }
}
