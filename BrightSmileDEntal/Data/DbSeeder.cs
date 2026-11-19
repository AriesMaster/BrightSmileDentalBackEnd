
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
            logger.LogInformation(
                "Demo user and profile seeding started."
            );

            // ==========================================
            // DEMO ACCOUNTS
            // ==========================================

            var demoUsers = new[]
            {
                new
                {
                    Email = "admin@brightsmile.com",
                    Password = "Demo@1234",
                    Role = UserRoles.Admin,

                    FirstName = "BrightSmile",
                    LastName = "Administrator",
                    PhoneNumber = "0820000001",
                    Gender = "",
                    DateOfBirth = (DateTime?)null
                },

                new
                {
                    Email = "dentist@brightsmile.com",
                    Password = "Demo@1234",
                    Role = UserRoles.Dentist,

                    FirstName = "Sarah",
                    LastName = "Mokoena",
                    PhoneNumber = "0820000002",
                    Gender = "Female",
                    DateOfBirth = (DateTime?)new DateTime(
                        1988, 6, 15,
                        0, 0, 0,
                        DateTimeKind.Utc
                    )
                },

                new
                {
                    Email = "patient@brightsmile.com",
                    Password = "Demo@1234",
                    Role = UserRoles.Patient,

                    FirstName = "John",
                    LastName = "Smith",
                    PhoneNumber = "0820000003",
                    Gender = "Male",
                    DateOfBirth = (DateTime?)new DateTime(
                        1999, 5, 21,
                        0, 0, 0,
                        DateTimeKind.Utc
                    )
                }
            };

            var hasChanges = false;

            // ==========================================
            // SEED USERS AND PROFILES
            // ==========================================

            foreach (var demo in demoUsers)
            {
                // Find existing user and profile.
                var user = await context.Users
                    .Include(u => u.Profile)
                    .SingleOrDefaultAsync(
                        u => u.Email == demo.Email
                    );

                // ======================================
                // CREATE USER IF MISSING
                // ======================================

                if (user == null)
                {
                    user = new User
                    {
                        Email = demo.Email,

                        PasswordHash =
                            BCrypt.Net.BCrypt.HashPassword(
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
                }

                // ======================================
                // UPDATE ROLE IF NECESSARY
                // ======================================

                else if (user.Role != demo.Role)
                {
                    user.Role = demo.Role;

                    hasChanges = true;

                    logger.LogInformation(
                        "Updated role for demo user {Email} to {Role}.",
                        demo.Email,
                        demo.Role
                    );
                }

                // ======================================
                // CREATE PROFILE IF MISSING
                // ======================================

                if (user.Profile == null)
                {
                    var profile = new Profile
                    {
                        User = user,

                        FirstName = demo.FirstName,
                        LastName = demo.LastName,

                        PhoneNumber = demo.PhoneNumber,

                        Gender = demo.Gender,

                        DateOfBirth = demo.DateOfBirth
                    };

                    context.Profiles.Add(profile);

                    hasChanges = true;

                    logger.LogInformation(
                        "Created profile for demo user {Email}.",
                        demo.Email
                    );
                }
                else
                {
                    logger.LogInformation(
                        "Profile already exists for {Email}. Skipping.",
                        demo.Email
                    );
                }
            }

            // ==========================================
            // SAVE CHANGES
            // ==========================================

            if (hasChanges)
            {
                await context.SaveChangesAsync();

                logger.LogInformation(
                    "Demo users and profiles saved successfully."
                );
            }
            else
            {
                logger.LogInformation(
                    "Demo users and profiles are already up to date. No changes required."
                );
            }

            logger.LogInformation(
                "Demo user and profile seeding finished."
            );
        }
    }
}
