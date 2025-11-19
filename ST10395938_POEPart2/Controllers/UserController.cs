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
        public IActionResult Login(string returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var user = await _userManager.FindByEmailAsync(vm.Email);
            if (user == null) { ModelState.AddModelError("", "Invalid login"); return View(vm); }

            var result = await _signInManager.PasswordSignInAsync(user.UserName, vm.Password, vm.RememberMe, lockoutOnFailure: false);
            if (!result.Succeeded) { ModelState.AddModelError("", "Invalid login"); return View(vm); }

            // set session values
            var roles = await _userManager.GetRolesAsync(user);
            HttpContext.Session.SetString("UserId", user.Id);
            HttpContext.Session.SetString("Role", roles.FirstOrDefault() ?? "");

            return LocalRedirect(vm.ReturnUrl ?? Url.Action("Index", "Home"));
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
