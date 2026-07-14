using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SRMSS.Web.Data;
using SRMSS.Web.Filters;
using SRMSS.Web.Utilities;
using SRMSS.Web.ViewModels;

namespace SRMSS.Web.Controllers
{
    [RoleAuthorize("SuperAdmin", "Admin", "User", "Customer")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            string role = HttpContext.Session.GetString(SessionKeys.Role) ?? string.Empty;
            string fullName = HttpContext.Session.GetString(SessionKeys.FullName) ?? "User";
            string username = HttpContext.Session.GetString(SessionKeys.Username) ?? string.Empty;

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

                ActiveTrips = await _context.Schedules
                    .CountAsync(s =>
                        s.Status == "On Time" ||
                        s.Status == "Departed"),

                DelayedTrips = await _context.Schedules
                    .CountAsync(s => s.Status == "Delayed"),

                CompletedTrips = await _context.Schedules
                    .CountAsync(s => s.Status == "Completed")
            };

            if (role == "SuperAdmin")
            {
                model.RecentAuditLogs = await _context.AuditLogs
                    .Include(a => a.AppUser)
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(5)
                    .ToListAsync();
            }

            return View(model);
        }
    }
}