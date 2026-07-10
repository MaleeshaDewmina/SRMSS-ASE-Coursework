using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SRMSS.Web.Data;

namespace SRMSS.Web.Controllers
{
    public class TripStatusController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TripStatusController(ApplicationDbContext context)
        {
            _context = context;
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
                schedulesQuery = schedulesQuery.Where(s => s.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                schedulesQuery = schedulesQuery.Where(s =>
                    s.TransportRoute.RouteName.Contains(search) ||
                    s.TransportRoute.StartPoint.Contains(search) ||
                    s.TransportRoute.EndPoint.Contains(search) ||
                    s.Driver.FullName.Contains(search) ||
                    s.Vehicle.RegistrationNumber.Contains(search));
            }

            var schedules = await schedulesQuery
                .OrderBy(s => s.ScheduleDate)
                .ThenBy(s => s.DepartureTime)
                .ToListAsync();

            ViewBag.SelectedStatus = status ?? "All";
            ViewBag.Search = search;

            ViewBag.TotalTrips = await _context.Schedules.CountAsync();
            ViewBag.ScheduledTrips = await _context.Schedules.CountAsync(s => s.Status == "Scheduled");
            ViewBag.DepartedTrips = await _context.Schedules.CountAsync(s => s.Status == "Departed");
            ViewBag.DelayedTrips = await _context.Schedules.CountAsync(s => s.Status == "Delayed");
            ViewBag.CompletedTrips = await _context.Schedules.CountAsync(s => s.Status == "Completed");
            ViewBag.CancelledTrips = await _context.Schedules.CountAsync(s => s.Status == "Cancelled");

            return View(schedules);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var schedule = await _context.Schedules.FindAsync(id);

            if (schedule == null)
            {
                return NotFound();
            }

            schedule.Status = status;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Trip status updated successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}