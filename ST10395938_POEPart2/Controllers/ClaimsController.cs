using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST10395938_POEPart2.Data;
using ST10395938_POEPart2.Models;

namespace ST10395938_POEPart2.Controllers
{
    [Authorize(Roles = "Lecturer,HR")]
    public class ClaimsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<Users> _userManager;

        private const int MaxMonthlyHours = 180; // max hours per month

        public ClaimsController(ApplicationDbContext db, IWebHostEnvironment env, UserManager<Users> userManager)
        {
            _db = db;
            _env = env;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

           
            var claims = await _db.LecturerClaims
                .Where(c => c.LecturerName == $"{user.FirstName} {user.LastName}")
                .OrderByDescending(c => c.CreateAt)
                .Take(5)
                .ToListAsync();

           
            ViewBag.LecturerFullName = $"{user.FirstName} {user.LastName}";
            ViewBag.ProfileImage = "/images/placeholder.jpg"; 

            return View(claims);
        }

        
        public async Task<IActionResult> MyClaims()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var claims = await _db.LecturerClaims
                .AsNoTracking()
                .Where(c => c.LecturerName == $"{user.FirstName} {user.LastName}")
                .OrderByDescending(c => c.CreateAt)
                .ToListAsync();

            return View(claims);
        }

        // Create new claim
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var model = new LecturerClaim
            {
                LecturerName = $"{user.FirstName} {user.LastName}",
                Rate = user.HourlyRate
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(decimal hoursWorked, IFormFile? evidence)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Validation
            if (hoursWorked <= 0 || hoursWorked > MaxMonthlyHours)
            {
                ModelState.AddModelError("HoursWorked", $"Hours must be between 1 and {MaxMonthlyHours} per month.");
                var model = new LecturerClaim
                {
                    LecturerName = $"{user.FirstName} {user.LastName}",
                    Rate = user.HourlyRate,
                    HoursWorked = hoursWorked
                };
                return View(model);
            }

            var claim = new LecturerClaim
            {
                LecturerName = $"{user.FirstName} {user.LastName}",
                Rate = user.HourlyRate,
                HoursWorked = hoursWorked,
                Amount = hoursWorked * user.HourlyRate,
                Status = "Pending",
                PaymentStatus = "Unpaid"
            };

            // Handle evidence file
            if (evidence != null && evidence.Length > 0)
            {
                var ext = Path.GetExtension(evidence.FileName).ToLowerInvariant();
                var allowed = new[] { ".pdf", ".docx" };
                if (!allowed.Contains(ext))
                {
                    ModelState.AddModelError("Evidence", "Only PDF and DOCX files are allowed.");
                    return View(claim);
                }

                const long maxSize = 5 * 1024 * 1024;
                if (evidence.Length > maxSize)
                {
                    ModelState.AddModelError("Evidence", "File size must be less than 5 MB.");
                    return View(claim);
                }

                var dir = Path.Combine(_env.WebRootPath, "uploads");
                Directory.CreateDirectory(dir);

                var uniqueName = $"{Guid.NewGuid():N}{ext}";
                using (var fs = System.IO.File.Create(Path.Combine(dir, uniqueName)))
                {
                    await evidence.CopyToAsync(fs);
                }

                claim.EvidenceFile = uniqueName;
            }

            _db.LecturerClaims.Add(claim);
            await _db.SaveChangesAsync();

            TempData["Message"] = "Claim submitted successfully.";
            return RedirectToAction(nameof(MyClaims));
        }

        // Edit a claim that was marked "Needs Fix"
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var claim = await _db.LecturerClaims.FindAsync(id);
            if (claim == null) return NotFound();

            if (claim.Status != "Needs Fix")
            {
                TempData["Message"] = "Only claims with status 'Needs Fix' can be edited.";
                return RedirectToAction(nameof(MyClaims));
            }

            return View(claim);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, decimal hoursWorked, IFormFile? evidence)
        {
            var claim = await _db.LecturerClaims.FindAsync(id);
            if (claim == null) return NotFound();

            if (claim.Status != "Needs Fix")
            {
                TempData["Message"] = "Only claims with status 'Needs Fix' can be edited.";
                return RedirectToAction(nameof(MyClaims));
            }

            // Validate hours
            if (hoursWorked <= 0 || hoursWorked > MaxMonthlyHours)
            {
                ModelState.AddModelError("HoursWorked", $"Hours must be between 1 and {MaxMonthlyHours} per month.");
                return View(claim);
            }

            // Update claim
            claim.HoursWorked = hoursWorked;
            var user = await _userManager.GetUserAsync(User);
            claim.Rate = user.HourlyRate;
            claim.Amount = hoursWorked * claim.Rate;
            claim.Status = "Pending";
            claim.ReviewNote = null;
            claim.ReviewedBy = null;
            claim.ReviewedAt = null;
            claim.PaymentStatus = "Unpaid";
            claim.PaymentReference = null;
            claim.PaidUTc = null;

            // Handle evidence
            if (evidence != null && evidence.Length > 0)
            {
                var ext = Path.GetExtension(evidence.FileName).ToLowerInvariant();
                var allowed = new[] { ".pdf", ".docx" };
                if (!allowed.Contains(ext))
                {
                    ModelState.AddModelError("Evidence", "Only PDF and DOCX files are allowed.");
                    return View(claim);
                }

                const long maxSize = 5 * 1024 * 1024;
                if (evidence.Length > maxSize)
                {
                    ModelState.AddModelError("Evidence", "File size must be less than 5 MB.");
                    return View(claim);
                }

                var dir = Path.Combine(_env.WebRootPath, "uploads");
                Directory.CreateDirectory(dir);

                var uniqueName = $"{Guid.NewGuid():N}{ext}";
                using (var fs = System.IO.File.Create(Path.Combine(dir, uniqueName)))
                {
                    await evidence.CopyToAsync(fs);
                }

                claim.EvidenceFile = uniqueName;
            }

            await _db.SaveChangesAsync();
            TempData["Message"] = "Claim updated and resubmitted for review.";

            return RedirectToAction(nameof(MyClaims));
        }
    }
}
