using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ST10395938_PROG6212_POEPart1.Models;

namespace ST10395938_PROG6212_POEPart1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult SubmitClaim()
        {
            return View();
        }

        public IActionResult TrackClaim()
        {
            return View();
        }

        public IActionResult CoordinatorApproval()
        {
            return View();
        }

        public IActionResult ManagerApproval()
        {
            return View();
        }

       public IActionResult Reject()
        {
            return View();
        }

        public IActionResult Reject2()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
