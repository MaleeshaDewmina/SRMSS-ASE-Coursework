using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SRMSS.Web.Data;
using SRMSS.Web.Filters;
using SRMSS.Web.Services;

namespace SRMSS.Web.Controllers
{
    [RoleAuthorize("SuperAdmin", "Admin", "User")]
    public class TripStatusController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _auditLogService;

        private static readonly string[] AllowedStatuses =
        {
            "Scheduled",
            "On Time",
            "Departed",
            "Delayed",
            "Completed",
            "Cancelled"
        };

        public TripStatusController(
            ApplicationDbContext context,
            AuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        public async Task<IActionResult> Index(string? status, string? search)
        {
            var schedulesQuery = _context.Schedules
                .Include(s => s.TransportRoute)
                .Include(s => s.Driver)
                .Include(s => s.Vehicle)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                if (!AllowedStatuses.Contains(status))
                {
                    return BadRequest("Invalid trip status filter.");
                }

                schedulesQuery = schedulesQuery.Where(s => s.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string cleanSearch = search.Trim();

                schedulesQuery = schedulesQuery.Where(s =>
                    (s.TransportRoute != null &&
                        (s.TransportRoute!.RouteName.Contains(cleanSearch) ||
                         s.TransportRoute!.StartPoint.Contains(cleanSearch) ||
                         s.TransportRoute!.EndPoint.Contains(cleanSearch))) ||
                    (s.Driver != null && s.Driver!.FullName.Contains(cleanSearch)) ||
                    (s.Vehicle != null && s.Vehicle!.RegistrationNumber.Contains(cleanSearch)));
            }

            var schedules = await schedulesQuery
                .OrderBy(s => s.ScheduleDate)
                .ThenBy(s => s.DepartureTime)
                .ToListAsync();

            ViewBag.SelectedStatus = status ?? "All";
            ViewBag.Search = search;
            ViewBag.TotalTrips = await _context.Schedules.CountAsync();
            ViewBag.ScheduledTrips = await _context.Schedules.CountAsync(s => s.Status == "Scheduled");
            ViewBag.OnTimeTrips = await _context.Schedules.CountAsync(s => s.Status == "On Time");
            ViewBag.DepartedTrips = await _context.Schedules.CountAsync(s => s.Status == "Departed");
            ViewBag.DelayedTrips = await _context.Schedules.CountAsync(s => s.Status == "Delayed");
            ViewBag.CompletedTrips = await _context.Schedules.CountAsync(s => s.Status == "Completed");
            ViewBag.CancelledTrips = await _context.Schedules.CountAsync(s => s.Status == "Cancelled");

            return View(schedules);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("SuperAdmin", "Admin")]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            if (!AllowedStatuses.Contains(status))
            {
                return BadRequest("Invalid trip status.");
            }

            var schedule = await _context.Schedules.FindAsync(id);

            if (schedule == null)
            {
                return NotFound();
            }

            string previousStatus = schedule.Status;
            schedule.Status = status;
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                "Update Trip Status",
                "Schedules",
                schedule.Id,
                $"Changed schedule #{schedule.Id} status from {previousStatus} to {status}"
            );

            TempData["SuccessMessage"] = "Trip status updated successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
