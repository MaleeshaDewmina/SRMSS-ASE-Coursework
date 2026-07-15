using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SRMSS.Web.Data;
using SRMSS.Web.Filters;
using SRMSS.Web.Models;
using SRMSS.Web.Services;

namespace SRMSS.Web.Controllers
{
    [RoleAuthorize("SuperAdmin", "Admin", "User")]
    public class RouteStopsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _auditLogService;

        public RouteStopsController(
            ApplicationDbContext context,
            AuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }


        // =========================================================
        // VIEW ROUTE STOPS
        // SuperAdmin, Admin and User
        // =========================================================

        public async Task<IActionResult> Index()
        {
            var routes = await _context.TransportRoutes
                .Include(r => r.RouteStops)
                .OrderByDescending(
                    r => r.RouteStops.Any()
                        ? r.RouteStops.Max(s => s.Id)
                        : 0
                )
                .ThenBy(r => r.RouteName)
                .ToListAsync();

            return View(routes);
        }


        // =========================================================
        // VIEW STOP DETAILS
        // SuperAdmin, Admin and User
        // =========================================================

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var routeStop = await _context.RouteStops
                .Include(r => r.TransportRoute)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (routeStop == null)
            {
                return NotFound();
            }

            return View(routeStop);
        }


        // =========================================================
        // CREATE STOP
        // SuperAdmin and Admin only
        // =========================================================

        [RoleAuthorize("SuperAdmin", "Admin")]
        public IActionResult Create()
        {
            LoadRoutesDropDown();

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("SuperAdmin", "Admin")]
        public async Task<IActionResult> Create(
            [Bind(
                "Id," +
                "TransportRouteId," +
                "StopName," +
                "StopOrder," +
                "EstimatedMinutesFromStart"
            )]
            RouteStop routeStop)
        {
            await ValidateStopOrder(routeStop);

            if (!ModelState.IsValid)
            {
                LoadRoutesDropDown(
                    routeStop.TransportRouteId
                );

                return View(routeStop);
            }

            _context.RouteStops.Add(routeStop);

            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                "Create Route Stop",
                "RouteStops",
                routeStop.Id,
                $"Added stop {routeStop.StopName} " +
                $"at order {routeStop.StopOrder} " +
                $"to route #{routeStop.TransportRouteId}"
            );

            TempData["SuccessMessage"] =
                "Route stop added successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // EDIT STOP
        // SuperAdmin and Admin only
        // =========================================================

        [RoleAuthorize("SuperAdmin", "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var routeStop = await _context.RouteStops
                .Include(r => r.TransportRoute)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (routeStop == null)
            {
                return NotFound();
            }

            LoadRoutesDropDown(
                routeStop.TransportRouteId
            );

            return View(routeStop);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("SuperAdmin", "Admin")]
        public async Task<IActionResult> Edit(
            int id,
            [Bind(
                "Id," +
                "TransportRouteId," +
                "StopName," +
                "StopOrder," +
                "EstimatedMinutesFromStart"
            )]
            RouteStop routeStop)
        {
            if (id != routeStop.Id)
            {
                return NotFound();
            }

            await ValidateStopOrder(
                routeStop,
                routeStop.Id
            );

            if (!ModelState.IsValid)
            {
                LoadRoutesDropDown(
                    routeStop.TransportRouteId
                );

                return View(routeStop);
            }

            try
            {
                _context.RouteStops.Update(routeStop);

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RouteStopExists(routeStop.Id))
                {
                    return NotFound();
                }

                throw;
            }

            await _auditLogService.LogAsync(
                "Update Route Stop",
                "RouteStops",
                routeStop.Id,
                $"Updated stop {routeStop.StopName} " +
                $"at order {routeStop.StopOrder} " +
                $"on route #{routeStop.TransportRouteId}"
            );

            TempData["SuccessMessage"] =
                "Route stop updated successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // DELETE STOP
        // SuperAdmin and Admin only
        // =========================================================

        [RoleAuthorize("SuperAdmin", "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var routeStop = await _context.RouteStops
                .Include(r => r.TransportRoute)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (routeStop == null)
            {
                return NotFound();
            }

            return View(routeStop);
        }


        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("SuperAdmin", "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var routeStop = await _context.RouteStops
                .FindAsync(id);

            if (routeStop == null)
            {
                return NotFound();
            }

            string stopName = routeStop.StopName;

            int routeId = routeStop.TransportRouteId;

            _context.RouteStops.Remove(routeStop);

            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                "Delete Route Stop",
                "RouteStops",
                id,
                $"Deleted stop {stopName} from route #{routeId}"
            );

            TempData["SuccessMessage"] =
                "Route stop deleted successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // PRIVATE HELPERS
        // =========================================================

        private async Task ValidateStopOrder(
            RouteStop routeStop,
            int? editingStopId = null)
        {
            bool routeExists = await _context.TransportRoutes
                .AnyAsync(
                    r => r.Id == routeStop.TransportRouteId
                );

            if (!routeExists)
            {
                ModelState.AddModelError(
                    "TransportRouteId",
                    "Selected route does not exist."
                );

                return;
            }

            bool duplicateStopOrder =
                await _context.RouteStops.AnyAsync(
                    rs =>
                        rs.Id != editingStopId &&
                        rs.TransportRouteId ==
                            routeStop.TransportRouteId &&
                        rs.StopOrder ==
                            routeStop.StopOrder
                );

            if (duplicateStopOrder)
            {
                ModelState.AddModelError(
                    "StopOrder",
                    "This stop order already exists for the selected route."
                );
            }
        }


        private bool RouteStopExists(int id)
        {
            return _context.RouteStops.Any(
                e => e.Id == id
            );
        }


        private void LoadRoutesDropDown(
            object? selectedRoute = null)
        {
            ViewData["TransportRouteId"] =
                new SelectList(
                    _context.TransportRoutes
                        .OrderBy(r => r.RouteName),
                    "Id",
                    "RouteName",
                    selectedRoute
                );
        }
    }
}