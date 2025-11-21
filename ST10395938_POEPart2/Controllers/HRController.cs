using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ST10395938_POEPart2.Data;
using ST10395938_POEPart2.Models;
using Microsoft.EntityFrameworkCore;

namespace ST10395938_POEPart2.Controllers
{
    [Authorize(Roles = "HR")]
    public class HRController : Controller
    {
        private readonly UserManager<Users> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _db;

        public HRController(UserManager<Users> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext db)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _db = db;
        }

        // HR Dashboard
        public async Task<IActionResult> Index()
        {
            var users = _userManager.Users.ToList();
            var userWithRoles = new List<UserWithRoleViewModel>();

            foreach (var user in users)
            {
                var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "N/A";
                userWithRoles.Add(new UserWithRoleViewModel
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    HourlyRate = user.HourlyRate,
                    Role = role
                });
            }

            return View(userWithRoles);
        }


        // Create new user
        [HttpGet]
        public IActionResult Create() => View(new CreateUserViewModel { HourlyRate = 0 });

        [HttpPost]
        public async Task<IActionResult> Create(CreateUserViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var user = new Users
            {
                UserName = vm.Email,
                Email = vm.Email,
                FirstName = vm.FirstName,
                LastName = vm.LastName,
                HourlyRate = vm.HourlyRate
            };

            var result = await _userManager.CreateAsync(user, vm.Password);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
                return View(vm);
            }

            if (!await _roleManager.RoleExistsAsync(vm.Role))
                await _roleManager.CreateAsync(new IdentityRole(vm.Role));

            await _userManager.AddToRoleAsync(user, vm.Role);

            TempData["Message"] = "User created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // Edit user
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var vm = new EditUserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                HourlyRate = user.HourlyRate,
                Role = (await _userManager.GetRolesAsync(user)).FirstOrDefault()
            };
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(EditUserViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var user = await _userManager.FindByIdAsync(vm.Id);
            if (user == null) return NotFound();

            user.FirstName = vm.FirstName;
            user.LastName = vm.LastName;
            user.HourlyRate = vm.HourlyRate;
            user.Email = vm.Email;
            user.UserName = vm.Email;
            await _userManager.UpdateAsync(user);

            // Update roles
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.FirstOrDefault() != vm.Role)
            {
                await _userManager.RemoveFromRolesAsync(user, roles);
                await _userManager.AddToRoleAsync(user, vm.Role);
            }

            TempData["Message"] = "User updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult GenerateReport()
        {
            var claims = _db.LecturerClaims.ToList();

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);

                    page.Header().Text("Lecturer Claims Report").FontSize(20).Bold().AlignCenter();

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(50);
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        // Header
                        table.Header(header =>
                        {
                            header.Cell().Text("ID").SemiBold();
                            header.Cell().Text("Lecturer").SemiBold();
                            header.Cell().Text("Hours").SemiBold();
                            header.Cell().Text("Rate").SemiBold();
                            header.Cell().Text("Amount").SemiBold();
                            header.Cell().Text("Status").SemiBold();
                            header.Cell().Text("Reviewed By").SemiBold();
                        });

                        // Rows
                        foreach (var c in claims)
                        {
                            table.Cell().Text(c.Id.ToString());
                            table.Cell().Text(c.LecturerName);
                            table.Cell().Text(c.HoursWorked.ToString("F2"));
                            table.Cell().Text($"R {c.Rate:N2}");
                            table.Cell().Text($"R {c.Amount:N2}");
                            table.Cell().Text(c.Status);
                            table.Cell().Text(c.ReviewedBy ?? "N/A");
                        }
                    });
                });
            });

            var pdfBytes = pdf.GeneratePdf();
            return File(pdfBytes, "application/pdf", "LecturerClaimsReport.pdf");
        }

    }
}
