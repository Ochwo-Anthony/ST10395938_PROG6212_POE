using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST10395938_POEPart2.Data;

namespace ST10395938_POEPart2.Controllers
{
    public class ManagerController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ManagerController(ApplicationDbContext db)
        {
            _db = db;
        }
        public async Task<IActionResult> Index(string? lecturerName)
        {
            var q = _db.LecturerClaims
                .AsNoTracking()
                .Where(x => x.Status == "Coordinator Approved");

            if (!string.IsNullOrWhiteSpace(lecturerName) )
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

            if (row == null ) return NotFound();

            row.Status = "Manager Approved";
            row.PaymentStatus = "Paid";
            row.PaymentReference = $"PAY-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
            row.PaidUTc = DateTime.UtcNow;
            row.ReviewNote = null;

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));


        }

        [HttpPost]
        public async Task<IActionResult> Reject(int id, string reason)
        {
            var row = await _db.LecturerClaims.FindAsync(id);

            if (row == null ) return NotFound();

            row.Status = "Needs Fix";
            row.ReviewNote = string.IsNullOrWhiteSpace(reason) ? "Changes Requested" : reason.Trim();
            row.ReviewedBy = "Manager";
            row.ReviewedAt = DateTime.UtcNow;
            row.PaymentStatus = "Unpaid";
            row.PaymentReference = null;
            row.PaidUTc = null;

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
