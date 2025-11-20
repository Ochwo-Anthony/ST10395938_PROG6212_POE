using Microsoft.AspNetCore.Identity;
using ST10395938_POEPart2.Models;

namespace ST10395938_POEPart2.Data
{
    public class SeedData
    {
        public static async Task EnsureSeededAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Users>>();

            // Define roles
            string[] roles = new[] { "HR", "Lecturer", "Coordinator", "Manager" };

            // Ensure roles exist
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Default users - use email as username for consistency
            var defaultUsers = new List<(string Email, string FirstName, string LastName, string Role, decimal HourlyRate, string Password)>
            {
                ("hr@example.com", "System", "HR", "HR", 0, "Password123!"),
                ("lecturer@example.com", "John", "Doe", "Lecturer", 100, "Password123!"),
                ("coordinator@example.com", "Jane", "Smith", "Coordinator", 0, "Password123!"),
                ("manager@example.com", "Mark", "Taylor", "Manager", 0, "Password123!")
            };

            foreach (var u in defaultUsers)
            {
                var existing = await userManager.FindByEmailAsync(u.Email);
                if (existing == null)
                {
                    var user = new Users
                    {
                        UserName = u.Email, // Use email as username
                        Email = u.Email,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        HourlyRate = u.HourlyRate,
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(user, u.Password);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, u.Role);
                        Console.WriteLine($"Created user: {u.Email} with role: {u.Role}");
                    }
                    else
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        Console.WriteLine($"Failed to create user {u.Email}: {errors}");
                    }
                }
                else
                {
                    Console.WriteLine($"User {u.Email} already exists");
                }
            }
        }
    }
}