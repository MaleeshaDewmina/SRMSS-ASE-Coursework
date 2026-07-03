using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SRMSS.Web.Data;
using SRMSS.Web.Models;

namespace SRMSS.Web.Controllers
{
    public class TransportRoutesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TransportRoutesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: TransportRoutes
        public async Task<IActionResult> Index()
        {
            return View(await _context.TransportRoutes.ToListAsync());
        }

        // GET: TransportRoutes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transportRoute = await _context.TransportRoutes
                .FirstOrDefaultAsync(m => m.Id == id);
            if (transportRoute == null)
            {
                return NotFound();
            }

            return View(transportRoute);
        }

        // GET: TransportRoutes/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: TransportRoutes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,RouteName,StartPoint,EndPoint,DistanceKm,EstimatedDurationMinutes,ServiceType,Status")] TransportRoute transportRoute)
        {
            if (ModelState.IsValid)
            {
                _context.Add(transportRoute);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(transportRoute);
        }

        // GET: TransportRoutes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transportRoute = await _context.TransportRoutes.FindAsync(id);
            if (transportRoute == null)
            {
                return NotFound();
            }
            return View(transportRoute);
        }

        // POST: TransportRoutes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,RouteName,StartPoint,EndPoint,DistanceKm,EstimatedDurationMinutes,ServiceType,Status")] TransportRoute transportRoute)
        {
            if (id != transportRoute.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(transportRoute);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TransportRouteExists(transportRoute.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(transportRoute);
        }

        // GET: TransportRoutes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transportRoute = await _context.TransportRoutes
                .FirstOrDefaultAsync(m => m.Id == id);
            if (transportRoute == null)
            {
                return NotFound();
            }

            return View(transportRoute);
        }

        // POST: TransportRoutes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var transportRoute = await _context.TransportRoutes.FindAsync(id);
            if (transportRoute != null)
            {
                _context.TransportRoutes.Remove(transportRoute);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TransportRouteExists(int id)
        {
            return _context.TransportRoutes.Any(e => e.Id == id);
        }
    }
}
