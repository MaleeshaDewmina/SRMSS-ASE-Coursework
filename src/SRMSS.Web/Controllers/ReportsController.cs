using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SRMSS.Web.Data;
using SRMSS.Web.Filters;
using SRMSS.Web.ViewModels;

namespace SRMSS.Web.Controllers
{
    [RoleAuthorize("SuperAdmin", "Admin", "User")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate)
        {
            DateTime reportFromDate = fromDate?.Date ?? DateTime.Today.AddDays(-30);
            DateTime reportToDate = toDate?.Date ?? DateTime.Today;

            var rangeFuelLogs = _context.FuelLogs
                .Where(f => f.FuelDate.Date >= reportFromDate && f.FuelDate.Date <= reportToDate);

            var rangeMaintenanceLogs = _context.MaintenanceLogs
                .Where(m => m.MaintenanceDate.Date >= reportFromDate && m.MaintenanceDate.Date <= reportToDate);

            var model = new ReportsViewModel
            {
                FromDate = reportFromDate,
                ToDate = reportToDate,
                TotalRoutes = await _context.TransportRoutes.CountAsync(),
                ActiveRoutes = await _context.TransportRoutes.CountAsync(r => r.Status == "Active"),
                TotalSchedules = await _context.Schedules.CountAsync(),
                CompletedTrips = await _context.Schedules.CountAsync(s => s.Status == "Completed"),
                DelayedTrips = await _context.Schedules.CountAsync(s => s.Status == "Delayed"),
                CancelledTrips = await _context.Schedules.CountAsync(s => s.Status == "Cancelled"),
                TotalDrivers = await _context.Drivers.CountAsync(d => d.IsActive),
                AvailableDrivers = await _context.Drivers.CountAsync(d => d.IsActive && d.Status == "Available"),
                TotalVehicles = await _context.Vehicles.CountAsync(v => v.IsActive),
                AvailableVehicles = await _context.Vehicles.CountAsync(v => v.IsActive && v.Status == "Available"),
                FuelCost = await rangeFuelLogs.SumAsync(f => (decimal?)f.Cost) ?? 0,
                FuelLitres = await rangeFuelLogs.SumAsync(f => (decimal?)f.Litres) ?? 0,
                AverageEfficiency = await rangeFuelLogs
                    .Where(f => f.FuelEfficiencyKmPerLitre > 0)
                    .AverageAsync(f => (decimal?)f.FuelEfficiencyKmPerLitre) ?? 0,
                MaintenanceCost = await rangeMaintenanceLogs.SumAsync(m => (decimal?)m.Cost) ?? 0,
                OpenFeedback = await _context.CustomerFeedbacks.CountAsync(f => f.Status == "Open" || f.Status == "In Review"),
                PublishedAnnouncements = await _context.Announcements.CountAsync(a => a.Status == "Published")
            };

            model.RoutePerformance = await _context.Schedules
                .Include(s => s.TransportRoute)
                .Where(s => s.ScheduleDate.Date >= reportFromDate && s.ScheduleDate.Date <= reportToDate)
                .GroupBy(s => s.TransportRoute != null ? s.TransportRoute.RouteName : "Unknown Route")
                .Select(g => new RoutePerformanceRow
                {
                    RouteName = g.Key,
                    TotalTrips = g.Count(),
                    CompletedTrips = g.Count(s => s.Status == "Completed"),
                    DelayedTrips = g.Count(s => s.Status == "Delayed"),
                    CancelledTrips = g.Count(s => s.Status == "Cancelled")
                })
                .OrderByDescending(r => r.TotalTrips)
                .Take(8)
                .ToListAsync();

            model.VehicleCost = await _context.Vehicles
                .Select(v => new VehicleCostRow
                {
                    Vehicle = v.RegistrationNumber,
                    FuelCost = v.FuelLogs
                        .Where(f => f.FuelDate.Date >= reportFromDate && f.FuelDate.Date <= reportToDate)
                        .Sum(f => (decimal?)f.Cost) ?? 0,
                    MaintenanceCost = v.MaintenanceLogs
                        .Where(m => m.MaintenanceDate.Date >= reportFromDate && m.MaintenanceDate.Date <= reportToDate)
                        .Sum(m => (decimal?)m.Cost) ?? 0
                })
                .OrderByDescending(v => v.FuelCost + v.MaintenanceCost)
                .Take(8)
                .ToListAsync();

            model.StatusBreakdown = await _context.Schedules
                .Where(s => s.ScheduleDate.Date >= reportFromDate && s.ScheduleDate.Date <= reportToDate)
                .GroupBy(s => s.Status)
                .Select(g => new StatusBreakdownRow
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(s => s.Count)
                .ToListAsync();

            return View(model);
        }
    }
}
