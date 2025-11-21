using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ST10395938_POEPart2.Models;

namespace ST10395938_POEPart2.Controllers
{
    public class UserController : Controller
    {
        private readonly SignInManager<Users> _signInManager;
        private readonly UserManager<Users> _userManager;

        public UserController(SignInManager<Users> signInManager, UserManager<Users> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
           
            HttpContext.Session.Clear();
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel vm, string returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var user = await _userManager.FindByEmailAsync(vm.Email);
            if (user == null)
            {
                
                user = await _userManager.FindByNameAsync(vm.Email);
                if (user == null)
                {
                    ModelState.AddModelError("", "Invalid login attempt.");
                    return View(vm);
                }
            }

           
            var result = await _signInManager.PasswordSignInAsync(
                user.UserName,
                vm.Password,
                vm.RememberMe,
                lockoutOnFailure: false);

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Invalid login attempt.");
                return View(vm);
            }

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "";

            HttpContext.Session.SetString("UserId", user.Id);
            HttpContext.Session.SetString("UserName", user.UserName);
            HttpContext.Session.SetString("Role", role);

           
            if (role == "HR")
                return RedirectToAction("Index", "HR");
            else if (role == "Lecturer")
                return RedirectToAction("Index", "Claims");
            else if (role == "Coordinator")
                return RedirectToAction("Index", "Coordinator");
            else if (role == "Manager")
                return RedirectToAction("Index", "Manager");
            else
                return RedirectToAction("Login"); // fallback
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
