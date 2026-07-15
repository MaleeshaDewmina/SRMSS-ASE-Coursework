using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SRMSS.Web.Data;
using SRMSS.Web.Filters;
using SRMSS.Web.Models;
using SRMSS.Web.Services;

namespace SRMSS.Web.Controllers
{
    [RoleAuthorize("SuperAdmin", "Admin", "User")]
    public class TransportRoutesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _auditLogService;

        public TransportRoutesController(
            ApplicationDbContext context,
            AuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }


        // =========================================================
        // VIEW ROUTES
        // SuperAdmin, Admin and User
        // =========================================================

        public async Task<IActionResult> Index()
        {
            var routes = await _context.TransportRoutes
                .Include(r => r.RouteStops)
                .OrderBy(r => r.RouteName)
                .ToListAsync();

            return View(routes);
        }


        // =========================================================
        // VIEW ROUTE DETAILS
        // SuperAdmin, Admin and User
        // =========================================================

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transportRoute = await _context.TransportRoutes
                .Include(r => r.RouteStops)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (transportRoute == null)
            {
                return NotFound();
            }

            return View(transportRoute);
        }


        // =========================================================
        // CREATE ROUTE
        // SuperAdmin and Admin only
        // =========================================================

        [RoleAuthorize("SuperAdmin", "Admin")]
        public IActionResult Create()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("SuperAdmin", "Admin")]
        public async Task<IActionResult> Create(
            [Bind(
                "Id," +
                "RouteName," +
                "StartPoint," +
                "EndPoint," +
                "DistanceKm," +
                "EstimatedDurationMinutes," +
                "ServiceType," +
                "Status"
            )]
            TransportRoute transportRoute)
        {
            if (!ModelState.IsValid)
            {
                return View(transportRoute);
            }

            _context.TransportRoutes.Add(transportRoute);

            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                "Create Route",
                "TransportRoutes",
                transportRoute.Id,
                $"Created route {transportRoute.RouteName}: " +
                $"{transportRoute.StartPoint} to {transportRoute.EndPoint}"
            );

            TempData["SuccessMessage"] =
                "Route created successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // EDIT ROUTE
        // SuperAdmin and Admin only
        // =========================================================

        [RoleAuthorize("SuperAdmin", "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transportRoute = await _context.TransportRoutes
                .Include(r => r.RouteStops)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (transportRoute == null)
            {
                return NotFound();
            }

            return View(transportRoute);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("SuperAdmin", "Admin")]
        public async Task<IActionResult> Edit(
            int id,
            [Bind(
                "Id," +
                "RouteName," +
                "StartPoint," +
                "EndPoint," +
                "DistanceKm," +
                "EstimatedDurationMinutes," +
                "ServiceType," +
                "Status"
            )]
            TransportRoute transportRoute)
        {
            if (id != transportRoute.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(transportRoute);
            }

            try
            {
                _context.TransportRoutes.Update(transportRoute);

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TransportRouteExists(transportRoute.Id))
                {
                    return NotFound();
                }

                throw;
            }

            await _auditLogService.LogAsync(
                "Update Route",
                "TransportRoutes",
                transportRoute.Id,
                $"Updated route {transportRoute.RouteName}: " +
                $"{transportRoute.StartPoint} to {transportRoute.EndPoint}"
            );

            TempData["SuccessMessage"] =
                "Route updated successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // DELETE ROUTE
        // SuperAdmin and Admin only
        // =========================================================

        [RoleAuthorize("SuperAdmin", "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transportRoute = await _context.TransportRoutes
                .Include(r => r.RouteStops)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (transportRoute == null)
            {
                return NotFound();
            }

            return View(transportRoute);
        }


        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("SuperAdmin", "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var transportRoute = await _context.TransportRoutes
                .Include(r => r.Schedules)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (transportRoute == null)
            {
                return NotFound();
            }

            if (transportRoute.Schedules.Any())
            {
                TempData["ErrorMessage"] =
                    "This route cannot be deleted because schedules are assigned to it. " +
                    "Remove or reassign those schedules first.";

                return RedirectToAction(nameof(Index));
            }

            string routeName = transportRoute.RouteName;

            _context.TransportRoutes.Remove(transportRoute);

            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                "Delete Route",
                "TransportRoutes",
                id,
                $"Deleted route {routeName}"
            );

            TempData["SuccessMessage"] =
                "Route deleted successfully.";

            return RedirectToAction(nameof(Index));
        }


        private bool TransportRouteExists(int id)
        {
            return _context.TransportRoutes.Any(
                r => r.Id == id
            );
        }
    }
}