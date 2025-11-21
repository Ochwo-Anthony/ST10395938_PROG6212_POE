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

        // Validation constants
        private const int MaxMonthlyHours = 180;
        private const decimal MaxHourlyRate = 1000;
        private const decimal MaxTotalAmount = 100000;

        public CoordinatorController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index(string? lecturerName)
        {
            var q = _db.LecturerClaims
                .AsNoTracking()
                .Where(x => x.Status == "Pending");

            if (!string.IsNullOrWhiteSpace(lecturerName))
            {
                var term = lecturerName.Trim();
                q = q.Where(x => EF.Functions.Like(x.LecturerName, $"%{term}%"));
            }

            var list = await q.OrderBy(x => x.CreateAt).ToListAsync();
            ViewBag.LecturerName = lecturerName ?? "";
            return View(list);
        }

        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var row = await _db.LecturerClaims.FindAsync(id);
            if (row == null) return NotFound();

            // Validate claim before approving
            if (!ValidateClaim(row, out string reason))
            {
                TempData["Message"] = $"Claim cannot be approved: {reason}";
                return RedirectToAction(nameof(Index));
            }

            row.Status = "Coordinator Approved";
            row.ReviewNote = null;

            await _db.SaveChangesAsync();
            TempData["Message"] = "Claim approved successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Reject(int id, string reason)
        {
            var row = await _db.LecturerClaims.FindAsync(id);
            if (row == null) return NotFound();

            row.Status = "Needs Fix";
            row.ReviewNote = string.IsNullOrWhiteSpace(reason) ? "Please fix and resubmit" : reason.Trim();
            row.ReviewedBy = "Coordinator";
            row.ReviewedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            TempData["Message"] = "Claim rejected.";
            return RedirectToAction(nameof(Index));
        }

        // Validation method
        private bool ValidateClaim(LecturerClaim claim, out string reason)
        {
            reason = string.Empty;

            if (claim.HoursWorked <= 0 || claim.HoursWorked > MaxMonthlyHours)
            {
                reason = $"Hours worked ({claim.HoursWorked}) exceed the maximum allowed per month ({MaxMonthlyHours}).";
                return false;
            }

            if (claim.Rate <= 0 || claim.Rate > MaxHourlyRate)
            {
                reason = $"Hourly rate ({claim.Rate:C}) exceeds maximum allowed ({MaxHourlyRate:C}).";
                return false;
            }

            if (claim.Amount != claim.HoursWorked * claim.Rate)
            {
                reason = $"Claim total ({claim.Amount:C}) does not match hours worked * hourly rate.";
                return false;
            }

            if (claim.Amount > MaxTotalAmount)
            {
                reason = $"Claim total ({claim.Amount:C}) exceeds maximum allowed ({MaxTotalAmount:C}).";
                return false;
            }

            return true; // All validations passed
        }
    }
}
