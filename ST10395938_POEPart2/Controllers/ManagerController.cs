using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST10395938_POEPart2.Data;
using ST10395938_POEPart2.Models;

namespace ST10395938_POEPart2.Controllers
{
    [Authorize(Roles = "Manager,HR")]
    public class ManagerController : Controller
    {
        private readonly ApplicationDbContext _db;

       
        private const int MaxMonthlyHours = 180;       
        private const decimal MaxHourlyRate = 400;    
        private const decimal MaxTotalAmount = 100000;

        public ManagerController(ApplicationDbContext db)
        {
            _db = db;
        }

        // View all claims pending manager approval
        public async Task<IActionResult> Index(string? lecturerName)
        {
            var query = _db.LecturerClaims
                .AsNoTracking()
                .Where(x => x.Status == "Coordinator Approved");

            if (!string.IsNullOrWhiteSpace(lecturerName))
            {
                var term = lecturerName.Trim();
                query = query.Where(x => EF.Functions.Like(x.LecturerName, $"%{term}%"));
            }

            var list = await query.OrderBy(x => x.CreateAt).ToListAsync();
            ViewBag.LecturerName = lecturerName ?? "";

            return View(list);
        }

        // Approve claim after validation
        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var claim = await _db.LecturerClaims.FindAsync(id);
            if (claim == null) return NotFound();

            // Automated validation
            if (!ValidateClaimForApproval(claim, out string reason))
            {
                TempData["Message"] = $"Claim cannot be approved: {reason}";
                return RedirectToAction(nameof(Index));
            }

            // Approve if all checks pass
            claim.Status = "Manager Approved";
            claim.PaymentStatus = "Paid";
            claim.PaymentReference = $"PAY-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
            claim.PaidUTc = DateTime.UtcNow;
            claim.ReviewNote = null;

            await _db.SaveChangesAsync();
            TempData["Message"] = "Claim successfully approved.";
            return RedirectToAction(nameof(Index));
        }

        // Reject claim with reason
        [HttpPost]
        public async Task<IActionResult> Reject(int id, string reason)
        {
            var claim = await _db.LecturerClaims.FindAsync(id);
            if (claim == null) return NotFound();

            claim.Status = "Needs Fix";
            claim.ReviewNote = string.IsNullOrWhiteSpace(reason) ? "Changes Requested" : reason.Trim();
            claim.ReviewedBy = "Manager";
            claim.ReviewedAt = DateTime.UtcNow;
            claim.PaymentStatus = "Unpaid";
            claim.PaymentReference = null;
            claim.PaidUTc = null;

            await _db.SaveChangesAsync();
            TempData["Message"] = "Claim rejected. Lecturer needs to fix it.";
            return RedirectToAction(nameof(Index));
        }

        // Automated validation method
        private bool ValidateClaimForApproval(LecturerClaim claim, out string reason)
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
                reason = $"Claim total ({claim.Amount:C}) exceeds the maximum allowed total ({MaxTotalAmount:C}).";
                return false;
            }

            

            return true; 
        }
    }
}
