using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST10395938_POEPart2.Data;
using ST10395938_POEPart2.Models;

namespace ST10395938_POEPart2.Controllers
{
    [Authorize(Roles = "Lecturer,HR,Coordinator,Manager")]
    public class ClaimsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<Users> _userManager;

        private const int MaxMonthlyHours = 180; // example limit

        public ClaimsController(ApplicationDbContext db, IWebHostEnvironment env, UserManager<Users> userManager)
        {
            _db = db;
            _env = env;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var claims = await _db.LecturerClaims.AsNoTracking().ToListAsync();
            return View(claims);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            // Pre-fill lecturer info if logged in
            if (User.Identity!.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    var model = new LecturerClaim
                    {
                        LecturerName = $"{user.FirstName} {user.LastName}",
                        Rate = user.HourlyRate
                    };
                    return View(model);
                }
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LecturerClaim model, IFormFile? evidence)
        {
            // Validate lecturer is logged in
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Pull rate from HR data
            model.LecturerName = $"{user.FirstName} {user.LastName}";
            model.Rate = user.HourlyRate;

            // Validate hours worked
            if (model.HoursWorked > MaxMonthlyHours)
            {
                ModelState.AddModelError("HoursWorked", $"Cannot submit more than {MaxMonthlyHours} hours per month.");
                return View(model);
            }

            if (!ModelState.IsValid) return View(model);

            // Handle evidence file
            if (evidence != null && evidence.Length > 0)
            {
                var ext = Path.GetExtension(evidence.FileName).ToLowerInvariant();
                var allowed = new[] { ".pdf", ".docx" };
                if (!allowed.Contains(ext))
                {
                    ModelState.AddModelError("Evidence", "Only PDF and DOCX files are allowed.");
                    return View(model);
                }

                const long max = 5 * 1024 * 1024;
                if (evidence.Length > max)
                {
                    ModelState.AddModelError("Evidence", "File size must be less than 5 MB.");
                    return View(model);
                }

                var dir = Path.Combine(_env.WebRootPath, "uploads");
                Directory.CreateDirectory(dir);

                var unique = $"{Guid.NewGuid():N}{ext}";
                using (var fs = System.IO.File.Create(Path.Combine(dir, unique)))
                {
                    await evidence.CopyToAsync(fs);
                }

                model.EvidenceFile = unique;
            }

            // Auto-calculate claim amount
            model.Amount = model.HoursWorked * model.Rate;

            model.Status = "Pending";
            model.PaymentStatus = "Unpaid";

            _db.LecturerClaims.Add(model);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(MyClaims), new { lecturerName = model.LecturerName });
        }

        [HttpGet]
        public async Task<IActionResult> MyClaims(string? lecturerName, string? status)
        {
            var q = _db.LecturerClaims.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(lecturerName))
            {
                var term = lecturerName.Trim();
                q = q.Where(m => EF.Functions.Like(m.LecturerName, $"%{term}%"));
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                q = q.Where(m => m.Status == status);
            }

            var items = await q.OrderByDescending(m => m.CreateAt).ToListAsync();

            ViewBag.LecturerName = lecturerName ?? "";
            ViewBag.Status = string.IsNullOrWhiteSpace(status) ? "All" : status;
            ViewBag.Statuses = new[] { "All", "Pending", "Needs Fix", "Coordinator Approved", "Manager Approved", "Rejected" };
            return View(items);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var row = await _db.LecturerClaims.FindAsync(id);

            if (row == null) return NotFound();

            if (row.Status != "Needs Fix")
            {
                TempData["Message"] = "Only logs with status 'Needs Fix' can be edited.";
                return RedirectToAction(nameof(MyClaims), new { lecturerName = row.LecturerName });
            }

            return View(row);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, LecturerClaim input, IFormFile? evidence)
        {
            var row = await _db.LecturerClaims.FindAsync(id);
            if (row == null) return NotFound();

            if (row.Status != "Needs Fix")
            {
                TempData["Message"] = "Only claims that need Fixing can be edited";
                return RedirectToAction(nameof(MyClaims), new { lecturerName = row.LecturerName });
            }

            // Validate max hours
            if (input.HoursWorked > MaxMonthlyHours)
            {
                ModelState.AddModelError("HoursWorked", $"Cannot submit more than {MaxMonthlyHours} hours per month.");
                return View(row);
            }

            if (!ModelState.IsValid) return View(row);

            row.HoursWorked = input.HoursWorked;
            row.Rate = input.Rate;
            row.Note = input.Note;
            row.Amount = row.HoursWorked * row.Rate;

            if (evidence != null && evidence.Length > 0)
            {
                var ext = Path.GetExtension(evidence.FileName).ToLowerInvariant();
                var allowed = new[] { ".pdf", ".docx" };
                if (!allowed.Contains(ext))
                {
                    ModelState.AddModelError("Evidence", "Only PDF and DOCX files are allowed.");
                    return View(row);
                }

                const long max = 5 * 1024 * 1024;
                if (evidence.Length > max)
                {
                    ModelState.AddModelError("Evidence", "File size must be less than 5 MB.");
                    return View(row);
                }

                var dir = Path.Combine(_env.WebRootPath, "uploads");
                Directory.CreateDirectory(dir);

                var unique = $"{Guid.NewGuid():N}{ext}";
                using (var fs = System.IO.File.Create(Path.Combine(dir, unique)))
                {
                    await evidence.CopyToAsync(fs);
                }

                row.EvidenceFile = unique;
            }

            row.Status = "Pending";
            row.ReviewNote = null;
            row.ReviewedBy = null;
            row.ReviewedAt = null;
            row.PaymentStatus = "Unpaid";
            row.PaymentReference = null;
            row.PaidUTc = null;

            await _db.SaveChangesAsync();

            TempData["Message"] = "Log updated successfully and is now pending review.";

            return RedirectToAction(nameof(MyClaims), new { lecturerName = row.LecturerName });
        }
    }
}
