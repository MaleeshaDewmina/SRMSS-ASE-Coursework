using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SRMSS.Web.Data;
using SRMSS.Web.Filters;
using SRMSS.Web.Utilities;
using SRMSS.Web.ViewModels;

namespace SRMSS.Web.Controllers
{
    [Route("Dashboard")]
    [RoleAuthorize("SuperAdmin", "Admin", "User", "Customer")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            string role = HttpContext.Session.GetString(SessionKeys.Role) ?? string.Empty;
            string fullName = HttpContext.Session.GetString(SessionKeys.FullName) ?? "User";
            string username = HttpContext.Session.GetString(SessionKeys.Username) ?? string.Empty;
            int? currentUserId = HttpContext.Session.GetInt32(SessionKeys.UserId);

            var model = new DashboardViewModel
            {
                FullName = fullName,
                Username = username,
                Role = role,

                TotalAdmins = await _context.AppUsers
                    .CountAsync(u => u.Role == "Admin"),

                TotalUsers = await _context.AppUsers
                    .CountAsync(u => u.Role == "User"),

                TotalCustomers = await _context.AppUsers
                    .CountAsync(u => u.Role == "Customer"),

                ActiveAccounts = await _context.AppUsers
                    .CountAsync(u => u.IsActive),

                InactiveAccounts = await _context.AppUsers
                    .CountAsync(u => !u.IsActive),

                TotalAuditLogs = await _context.AuditLogs.CountAsync(),

                TotalRoutes = await _context.TransportRoutes.CountAsync(),

                TotalSchedules = await _context.Schedules.CountAsync(),

                TotalDrivers = await _context.Drivers.CountAsync(),

                TotalVehicles = await _context.Vehicles.CountAsync(),

                AvailableRoutes = await _context.TransportRoutes
                    .CountAsync(r => r.Status == "Active"),

                AvailableSchedules = await _context.Schedules
                    .CountAsync(s =>
                        s.Status != "Cancelled" &&
                        s.ScheduleDate >= DateTime.Today),

                FavoriteRoutes = currentUserId.HasValue
                    ? await _context.FavoriteRoutes
                        .CountAsync(f =>
                            f.CustomerKey == currentUserId.Value.ToString())
                    : 0,

                ActiveTrips = await _context.Schedules
                    .CountAsync(s =>
                        s.Status == "On Time" ||
                        s.Status == "Departed"),

                DelayedTrips = await _context.Schedules
                    .CountAsync(s => s.Status == "Delayed"),

                CompletedTrips = await _context.Schedules
                    .CountAsync(s => s.Status == "Completed")
            };

            DateTime startDate = DateTime.Today.AddDays(-6);

            var recentActivityData = await _context.AuditLogs
                .Where(a => a.CreatedAt >= startDate)
                .GroupBy(a => a.CreatedAt.Date)
                .Select(group => new
                {
                    Date = group.Key,
                    Count = group.Count()
                })
                .ToListAsync();

            for (int i = 0; i < 7; i++)
            {
                DateTime date = startDate.AddDays(i);

                int count = recentActivityData
                    .FirstOrDefault(a => a.Date == date.Date)
                    ?.Count ?? 0;

                model.ActivityLabels.Add(date.ToString("ddd"));
                model.ActivityCounts.Add(count);
            }

            model.RecentAuditLogs = await _context.AuditLogs
                .Include(a => a.AppUser)
                .OrderByDescending(a => a.CreatedAt)
                .Take(6)
                .ToListAsync();

            return View(model);
        }
    }
}