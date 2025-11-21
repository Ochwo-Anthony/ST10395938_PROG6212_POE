using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST10395938_POEPart2.Data;
using ST10395938_POEPart2.Models;

namespace ST10395938_POEPart2.Controllers
{
    [Authorize(Roles = "Coordinator,HR")]
    public class CoordinatorController : Controller
    {
        private readonly ApplicationDbContext _db;

        // Validation limits
        private const int MaxMonthlyHours = 180;
        private const decimal MaxHourlyRate = 1000;
        private const decimal MaxTotalAmount = 100000;

        public CoordinatorController(ApplicationDbContext db)
        {
            _db = db;
        }

       
        public async Task<IActionResult> Index(string? lecturerName)
        {
            var query = _db.LecturerClaims
                .Where(x => x.Status == "Pending")
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(lecturerName))
            {
                var name = lecturerName.Trim();
                query = query.Where(x =>
                    EF.Functions.Like(x.LecturerName, $"%{name}%"));
            }

            ViewBag.LecturerName = lecturerName ?? "";
            var list = await query.OrderBy(x => x.CreateAt).ToListAsync();
            return View(list);
        }


        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var claim = await _db.LecturerClaims.FindAsync(id);
            if (claim == null)
                return NotFound();

            // Validate before approving
            if (!ValidateClaim(claim, out string errorMessage))
            {
                TempData["Message"] = $"Claim cannot be approved: {errorMessage}";
                return RedirectToAction(nameof(Index));
            }

            claim.Status = "Coordinator Approved";
            claim.ReviewNote = null;
            claim.ReviewedBy = "Coordinator";
            claim.ReviewedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            TempData["Message"] = "Claim approved successfully.";
            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        public async Task<IActionResult> Reject(int id, string reason)
        {
            var claim = await _db.LecturerClaims.FindAsync(id);
            if (claim == null)
                return NotFound();

            claim.Status = "Needs Fix";
            claim.ReviewedBy = "Coordinator";
            claim.ReviewedAt = DateTime.UtcNow;
            claim.ReviewNote = string.IsNullOrWhiteSpace(reason)
                ? "Please fix and resubmit"
                : reason.Trim();

            await _db.SaveChangesAsync();

            TempData["Message"] = "Claim rejected.";
            return RedirectToAction(nameof(Index));
        }


        private bool ValidateClaim(LecturerClaim claim, out string message)
        {
            message = string.Empty;

            // Hours validation
            if (claim.HoursWorked <= 0 || claim.HoursWorked > MaxMonthlyHours)
            {
                message = $"Hours worked ({claim.HoursWorked}) exceed the maximum allowed ({MaxMonthlyHours}).";
                return false;
            }

            // Rate validation
            if (claim.Rate <= 0 || claim.Rate > MaxHourlyRate)
            {
                message = $"Hourly rate (R{claim.Rate}) exceeds maximum allowed (R{MaxHourlyRate}).";
                return false;
            }

            // Amount validation
            decimal correctAmount = claim.HoursWorked * claim.Rate;
            if (claim.Amount != correctAmount)
            {
                message = $"Claim total (R{claim.Amount}) does not match hours worked × rate (R{correctAmount}).";
                return false;
            }

            if (claim.Amount > MaxTotalAmount)
            {
                message = $"Claim total (R{claim.Amount}) exceeds maximum allowed (R{MaxTotalAmount}).";
                return false;
            }

            return true;
        }
    }
}
