using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST10395938_POEPart2.Data;

namespace ST10395938_POEPart2.Controllers
{
    public class CoordinatorController : Controller
    {

        private readonly ApplicationDbContext _db;
        public CoordinatorController(ApplicationDbContext db)
        {
            _db = db;
        }
        public async Task<IActionResult> Index(string? lecturerName)
        {
            var q = _db.LecturerClaims
                .AsNoTracking()
                .Where(x => x.Status == "Pending");

            if (!String.IsNullOrWhiteSpace(lecturerName))
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
            row.Status = "Coordinator Approved";

            row.ReviewNote = null;
            await _db.SaveChangesAsync();

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
            return RedirectToAction(nameof(Index));

        }
    }
}
