
using BrightSmileDEntal.Constants;
using BrightSmileDEntal.Models;
using Microsoft.EntityFrameworkCore;

namespace BrightSmileDEntal.Data
{
    public static class DbSeeder
    {
        public static async Task SeedDemoUsersAsync(
            ApplicationDbContext context,
            ILogger logger)
        {
            logger.LogInformation("Demo user seeding started.");

            var demoUsers = new[]
            {
                new
                {
                    Email = "admin@brightsmile.com",
                    Password = "Demo@1234",
                    Role = UserRoles.Admin
                },
                new
                {
                    Email = "dentist@brightsmile.com",
                    Password = "Demo@1234",
                    Role = UserRoles.Dentist
                },
                new
                {
                    Email = "patient@brightsmile.com",
                    Password = "Demo@1234",
                    Role = UserRoles.Patient
                }
            };

            var hasChanges = false;

            foreach (var demo in demoUsers)
            {
                var user = await context.Users
                    .SingleOrDefaultAsync(u => u.Email == demo.Email);

                if (user == null)
                {
                    user = new User
                    {
                        Email = demo.Email,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(
                            demo.Password
                        ),
                        Role = demo.Role
                    };

                    context.Users.Add(user);
                    hasChanges = true;

                    logger.LogInformation(
                        "Created demo user {Email} with role {Role}.",
                        demo.Email,
                        demo.Role
                    );

                    continue;
                }

                // Correct the role only when necessary.
                if (user.Role != demo.Role)
                {
                    user.Role = demo.Role;
                    hasChanges = true;

                    logger.LogInformation(
                        "Updated role for demo user {Email} to {Role}.",
                        demo.Email,
                        demo.Role
                    );
                }
            }

            if (hasChanges)
            {
                await context.SaveChangesAsync();

                logger.LogInformation(
                    "Demo user changes saved successfully."
                );
            }
            else
            {
                logger.LogInformation(
                    "Demo users are already up to date. No changes required."
                );
            }

            logger.LogInformation("Demo user seeding finished.");
        }
    }
}
