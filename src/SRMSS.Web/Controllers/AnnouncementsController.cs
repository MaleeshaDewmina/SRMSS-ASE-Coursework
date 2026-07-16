using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SRMSS.Web.Data;
using SRMSS.Web.Filters;
using SRMSS.Web.Models;
using SRMSS.Web.Services;
using SRMSS.Web.Utilities;

namespace SRMSS.Web.Controllers
{
    [RoleAuthorize("SuperAdmin", "Admin", "User", "Customer")]
    public class AnnouncementsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _auditLogService;

        public AnnouncementsController(
            ApplicationDbContext context,
            AuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        public async Task<IActionResult> Index(string? audience, string? priority, string? search)
        {
            string role = HttpContext.Session.GetString(SessionKeys.Role) ?? string.Empty;
            bool canManage = role == "SuperAdmin" || role == "Admin";

            IQueryable<Announcement> announcements = _context.Announcements.AsQueryable();

            if (!canManage)
            {
                announcements = announcements.Where(a =>
                    a.Status == "Published" &&
                    a.PublishDate.Date <= DateTime.Today &&
                    (a.ExpiryDate == null || a.ExpiryDate.Value.Date >= DateTime.Today) &&
                    (a.Audience == "All" || a.Audience == role));
            }

            if (!string.IsNullOrWhiteSpace(audience))
            {
                announcements = announcements.Where(a => a.Audience == audience);
            }

            if (!string.IsNullOrWhiteSpace(priority))
            {
                announcements = announcements.Where(a => a.Priority == priority);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string cleanSearch = search.Trim();
                announcements = announcements.Where(a => a.Title.Contains(cleanSearch) || a.Message.Contains(cleanSearch));
            }

            ViewBag.Audience = audience;
            ViewBag.Priority = priority;
            ViewBag.Search = search;
            ViewBag.CanManage = canManage;
            ViewBag.TotalAnnouncements = await _context.Announcements.CountAsync();
            ViewBag.PublishedAnnouncements = await _context.Announcements.CountAsync(a => a.Status == "Published");
            ViewBag.HighPriorityAnnouncements = await _context.Announcements.CountAsync(a => a.Priority == "High" || a.Priority == "Critical");
            ViewBag.ExpiredAnnouncements = await _context.Announcements.CountAsync(a => a.ExpiryDate != null && a.ExpiryDate < DateTime.Today);

            return View(await announcements
                .OrderByDescending(a => a.Priority == "Critical")
                .ThenByDescending(a => a.PublishDate)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var announcement = await _context.Announcements
                .FirstOrDefaultAsync(a => a.Id == id);

            if (announcement == null)
            {
                return NotFound();
            }

            string role = HttpContext.Session.GetString(SessionKeys.Role) ?? string.Empty;
            bool canManage = role == "SuperAdmin" || role == "Admin";

            if (!canManage)
            {
                bool visible =
                    announcement.Status == "Published" &&
                    announcement.PublishDate.Date <= DateTime.Today &&
                    (announcement.ExpiryDate == null || announcement.ExpiryDate.Value.Date >= DateTime.Today) &&
                    (announcement.Audience == "All" || announcement.Audience == role);

                if (!visible)
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
            }

            ViewBag.CanManage = canManage;
            return View(announcement);
        }

        [RoleAuthorize("SuperAdmin", "Admin")]
        public IActionResult Create()
        {
            return View(new Announcement
            {
                Audience = "All",
                Priority = "Normal",
                Status = "Published",
                PublishDate = DateTime.Today
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("SuperAdmin", "Admin")]
        public async Task<IActionResult> Create(
            [Bind("Id,Title,Message,Audience,Priority,Status,PublishDate,ExpiryDate")]
            Announcement announcement)
        {
            ValidateAnnouncement(announcement);

            if (ModelState.IsValid)
            {
                announcement.CreatedAt = DateTime.Now;
                announcement.CreatedBy = HttpContext.Session.GetString(SessionKeys.FullName) ?? "SRMSS Admin";

                _context.Announcements.Add(announcement);
                await _context.SaveChangesAsync();

                await _auditLogService.LogAsync(
                    "Create Announcement",
                    "Announcements",
                    announcement.Id,
                    $"Published announcement: {announcement.Title}.");

                TempData["SuccessMessage"] = "Announcement created successfully.";

                return RedirectToAction(nameof(Index));
            }

            return View(announcement);
        }

        [RoleAuthorize("SuperAdmin", "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var announcement = await _context.Announcements.FindAsync(id);

            if (announcement == null)
            {
                return NotFound();
            }

            return View(announcement);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("SuperAdmin", "Admin")]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,Title,Message,Audience,Priority,Status,PublishDate,ExpiryDate,CreatedBy,CreatedAt")]
            Announcement announcement)
        {
            if (id != announcement.Id)
            {
                return NotFound();
            }

            ValidateAnnouncement(announcement);

            if (ModelState.IsValid)
            {
                try
                {
                    announcement.UpdatedAt = DateTime.Now;
                    _context.Update(announcement);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AnnouncementExists(announcement.Id))
                    {
                        return NotFound();
                    }

                    throw;
                }

                await _auditLogService.LogAsync(
                    "Update Announcement",
                    "Announcements",
                    announcement.Id,
                    $"Updated announcement: {announcement.Title}.");

                TempData["SuccessMessage"] = "Announcement updated successfully.";

                return RedirectToAction(nameof(Index));
            }

            return View(announcement);
        }

        [RoleAuthorize("SuperAdmin", "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var announcement = await _context.Announcements
                .FirstOrDefaultAsync(a => a.Id == id);

            if (announcement == null)
            {
                return NotFound();
            }

            return View(announcement);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("SuperAdmin", "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var announcement = await _context.Announcements.FindAsync(id);

            if (announcement == null)
            {
                return NotFound();
            }

            _context.Announcements.Remove(announcement);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                "Delete Announcement",
                "Announcements",
                id,
                $"Deleted announcement #{id}.");

            TempData["SuccessMessage"] = "Announcement deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        private void ValidateAnnouncement(Announcement announcement)
        {
            if (announcement.ExpiryDate != null && announcement.ExpiryDate < announcement.PublishDate)
            {
                ModelState.AddModelError("ExpiryDate", "Expiry date cannot be earlier than publish date.");
            }
        }

        private bool AnnouncementExists(int id)
        {
            return _context.Announcements.Any(e => e.Id == id);
        }
    }
}
