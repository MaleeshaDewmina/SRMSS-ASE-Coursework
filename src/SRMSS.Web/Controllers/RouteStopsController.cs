using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SRMSS.Web.Data;
using SRMSS.Web.Models;

namespace SRMSS.Web.Controllers
{
    public class RouteStopsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RouteStopsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: RouteStops
        public async Task<IActionResult> Index()
        {
            var routeStops = _context.RouteStops
                .Include(r => r.TransportRoute)
                .OrderBy(r => r.TransportRoute!.RouteName)
                .ThenBy(r => r.StopOrder);

            return View(await routeStops.ToListAsync());
        }

        // GET: RouteStops/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var routeStop = await _context.RouteStops
                .Include(r => r.TransportRoute)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (routeStop == null)
            {
                return NotFound();
            }

            return View(routeStop);
        }

        // GET: RouteStops/Create
        public IActionResult Create()
        {
            LoadRoutesDropDown();
            return View();
        }

        // POST: RouteStops/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,TransportRouteId,StopName,StopOrder,EstimatedMinutesFromStart")] RouteStop routeStop)
        {
            bool duplicateStopOrder = await _context.RouteStops.AnyAsync(rs =>
                rs.TransportRouteId == routeStop.TransportRouteId &&
                rs.StopOrder == routeStop.StopOrder
            );

            if (duplicateStopOrder)
            {
                ModelState.AddModelError("StopOrder", "This stop order already exists for the selected route.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(routeStop);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            LoadRoutesDropDown(routeStop.TransportRouteId);
            return View(routeStop);
        }

        // GET: RouteStops/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var routeStop = await _context.RouteStops.FindAsync(id);

            if (routeStop == null)
            {
                return NotFound();
            }

            LoadRoutesDropDown(routeStop.TransportRouteId);
            return View(routeStop);
        }

        // POST: RouteStops/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,TransportRouteId,StopName,StopOrder,EstimatedMinutesFromStart")] RouteStop routeStop)
        {
            if (id != routeStop.Id)
            {
                return NotFound();
            }

            bool duplicateStopOrder = await _context.RouteStops.AnyAsync(rs =>
                rs.Id != routeStop.Id &&
                rs.TransportRouteId == routeStop.TransportRouteId &&
                rs.StopOrder == routeStop.StopOrder
            );

            if (duplicateStopOrder)
            {
                ModelState.AddModelError("StopOrder", "This stop order already exists for the selected route.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(routeStop);
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

                return RedirectToAction(nameof(Index));
            }

            LoadRoutesDropDown(routeStop.TransportRouteId);
            return View(routeStop);
        }

        // GET: RouteStops/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var routeStop = await _context.RouteStops
                .Include(r => r.TransportRoute)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (routeStop == null)
            {
                return NotFound();
            }

            return View(routeStop);
        }

        // POST: RouteStops/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var routeStop = await _context.RouteStops.FindAsync(id);

            if (routeStop != null)
            {
                _context.RouteStops.Remove(routeStop);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool RouteStopExists(int id)
        {
            return _context.RouteStops.Any(e => e.Id == id);
        }

        private void LoadRoutesDropDown(object? selectedRoute = null)
        {
            ViewData["TransportRouteId"] = new SelectList(
                _context.TransportRoutes.OrderBy(r => r.RouteName),
                "Id",
                "RouteName",
                selectedRoute
            );
        }
    }
}