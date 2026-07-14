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
            int? userId = HttpContext.Session.GetInt32(SessionKeys.UserId);

            var model = new DashboardViewModel
            {
                FullName = fullName,
                Username = username,
                Role = role
            };

            switch (role)
            {
                case "SuperAdmin":
                    model.TotalAdmins = await _context.AppUsers.CountAsync(u => u.Role == "Admin");
                    model.TotalUsers = await _context.AppUsers.CountAsync(u => u.Role == "User");
                    model.TotalCustomers = await _context.AppUsers.CountAsync(u => u.Role == "Customer");
                    model.ActiveAccounts = await _context.AppUsers.CountAsync(u => u.IsActive);
                    model.InactiveAccounts = await _context.AppUsers.CountAsync(u => !u.IsActive);
                    model.TotalAuditLogs = await _context.AuditLogs.CountAsync();
                    model.TotalRoutes = await _context.TransportRoutes.CountAsync();
                    model.TotalSchedules = await _context.Schedules.CountAsync();

                    model.RecentAuditLogs = await _context.AuditLogs
                        .Include(a => a.AppUser)
                        .OrderByDescending(a => a.CreatedAt)
                        .Take(5)
                        .ToListAsync();
                    break;

                case "Admin":
                    model.TotalRoutes = await _context.TransportRoutes.CountAsync();
                    model.TotalSchedules = await _context.Schedules.CountAsync();
                    model.TotalDrivers = await _context.Drivers.CountAsync();
                    model.TotalVehicles = await _context.Vehicles.CountAsync();
                    model.ActiveTrips = await _context.Schedules.CountAsync(s =>
                        s.Status == "On Time" || s.Status == "Departed");
                    model.DelayedTrips = await _context.Schedules.CountAsync(s => s.Status == "Delayed");
                    model.CompletedTrips = await _context.Schedules.CountAsync(s => s.Status == "Completed");
                    model.TotalCustomers = await _context.AppUsers.CountAsync(u => u.Role == "Customer");
                    break;

                case "User":
                    model.TotalSchedules = await _context.Schedules.CountAsync();
                    model.ActiveTrips = await _context.Schedules.CountAsync(s =>
                        s.Status == "On Time" || s.Status == "Departed");
                    model.DelayedTrips = await _context.Schedules.CountAsync(s => s.Status == "Delayed");
                    model.CompletedTrips = await _context.Schedules.CountAsync(s => s.Status == "Completed");
                    break;

                case "Customer":
                    model.AvailableRoutes = await _context.TransportRoutes.CountAsync(r => r.Status == "Active");
                    model.AvailableSchedules = await _context.Schedules.CountAsync(s =>
                        s.TransportRoute != null &&
                        s.TransportRoute!.Status == "Active" &&
                        s.Status != "Cancelled");
                    model.ActiveTrips = await _context.Schedules.CountAsync(s =>
                        s.Status == "On Time" || s.Status == "Departed");
                    model.DelayedTrips = await _context.Schedules.CountAsync(s => s.Status == "Delayed");

                    if (userId.HasValue)
                    {
                        string customerKey = $"CUSTOMER:{userId.Value}";
                        model.FavoriteRoutes = await _context.FavoriteRoutes
                            .CountAsync(f => f.CustomerKey == customerKey);
                    }
                    break;
            }

            return View(model);
        }
    }
}
