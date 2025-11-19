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

            string[] roles = new[] { "HR", "Lecturer", "Coordinator", "Manager" };
            foreach (var r in roles)
                if (!await roleManager.RoleExistsAsync(r))
                    await roleManager.CreateAsync(new IdentityRole(r));

            // create initial HR user if not exists
            var hrEmail = "hr@example.com";
            var hr = await userManager.FindByEmailAsync(hrEmail);
            if (hr == null)
            {
                hr = new Users
                {
                    UserName = "hr",
                    Email = hrEmail,
                    FirstName = "System",
                    LastName = "HR",
                    HourlyRate = 0
                };
                var res = await userManager.CreateAsync(hr, "HrP@ssw0rd!"); // change password in prod
                if (res.Succeeded)
                    await userManager.AddToRoleAsync(hr, "HR");
            }
        }
    }
}
