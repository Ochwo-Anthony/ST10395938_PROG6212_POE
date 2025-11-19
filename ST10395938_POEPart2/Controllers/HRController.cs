using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ST10395938_POEPart2.Models;

namespace ST10395938_POEPart2.Controllers
{
    [Authorize(Roles = "HR")]
    public class HRController : Controller
    {
        private readonly UserManager<Users> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public HRController(UserManager<Users> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public IActionResult Index()
        {
            var users = _userManager.Users.ToList();
            return View(users);
        }

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
            if (!result.Succeeded) { foreach (var e in result.Errors) ModelState.AddModelError("", e.Description); return View(vm); }

            // assign role
            if (!await _roleManager.RoleExistsAsync(vm.Role))
                await _roleManager.CreateAsync(new IdentityRole(vm.Role));
            await _userManager.AddToRoleAsync(user, vm.Role);

            TempData["Message"] = "User created successfully.";
            return RedirectToAction(nameof(Index));
        }

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

            // update role
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.FirstOrDefault() != vm.Role)
            {
                await _userManager.RemoveFromRolesAsync(user, roles);
                await _userManager.AddToRoleAsync(user, vm.Role);
            }

            TempData["Message"] = "User updated.";
            return RedirectToAction(nameof(Index));
        }
    }
}
