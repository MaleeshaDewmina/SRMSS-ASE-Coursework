using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SRMSS.Web.Data;
using SRMSS.Web.Models;

namespace SRMSS.Web.Controllers
{
    public class SchedulesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SchedulesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Schedules
        public async Task<IActionResult> Index()
        {
            var schedules = _context.Schedules
                .Include(s => s.TransportRoute)
                .Include(s => s.Driver)
                .Include(s => s.Vehicle)
                .OrderBy(s => s.ScheduleDate)
                .ThenBy(s => s.DepartureTime);

            return View(await schedules.ToListAsync());
        }

        // GET: Schedules/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var schedule = await _context.Schedules
                .Include(s => s.TransportRoute)
                .Include(s => s.Driver)
                .Include(s => s.Vehicle)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (schedule == null)
            {
                return NotFound();
            }

            return View(schedule);
        }

        // GET: Schedules/Create
        public IActionResult Create()
        {
            LoadDropdowns();
            return View();
        }

        // POST: Schedules/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,TransportRouteId,VehicleId,DriverId,ScheduleDate,DepartureTime,ArrivalTime,Status,Notes")] Schedule schedule)
        {
            await ValidateScheduleConflicts(schedule);

            if (ModelState.IsValid)
            {
                _context.Add(schedule);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            LoadDropdowns(schedule.TransportRouteId, schedule.DriverId, schedule.VehicleId);
            return View(schedule);
        }

        // GET: Schedules/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var schedule = await _context.Schedules.FindAsync(id);

            if (schedule == null)
            {
                return NotFound();
            }

            LoadDropdowns(schedule.TransportRouteId, schedule.DriverId, schedule.VehicleId);
            return View(schedule);
        }

        // POST: Schedules/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,TransportRouteId,VehicleId,DriverId,ScheduleDate,DepartureTime,ArrivalTime,Status,Notes")] Schedule schedule)
        {
            if (id != schedule.Id)
            {
                return NotFound();
            }

            await ValidateScheduleConflicts(schedule, schedule.Id);

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(schedule);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ScheduleExists(schedule.Id))
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            LoadDropdowns(schedule.TransportRouteId, schedule.DriverId, schedule.VehicleId);
            return View(schedule);
        }

        // GET: Schedules/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var schedule = await _context.Schedules
                .Include(s => s.TransportRoute)
                .Include(s => s.Driver)
                .Include(s => s.Vehicle)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (schedule == null)
            {
                return NotFound();
            }

            return View(schedule);
        }

        // POST: Schedules/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var schedule = await _context.Schedules.FindAsync(id);

            if (schedule != null)
            {
                _context.Schedules.Remove(schedule);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Schedules/UpdateStatus
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

            return RedirectToAction(nameof(Index));
        }

        private async Task ValidateScheduleConflicts(Schedule schedule, int? editingScheduleId = null)
        {
            if (schedule.ArrivalTime <= schedule.DepartureTime)
            {
                ModelState.AddModelError("ArrivalTime", "Arrival time must be after departure time.");
            }

            bool driverConflict = await _context.Schedules.AnyAsync(s =>
                s.Id != editingScheduleId &&
                s.DriverId == schedule.DriverId &&
                s.ScheduleDate.Date == schedule.ScheduleDate.Date &&
                s.Status != "Cancelled" &&
                schedule.DepartureTime < s.ArrivalTime &&
                schedule.ArrivalTime > s.DepartureTime
            );

            if (driverConflict)
            {
                ModelState.AddModelError("DriverId", "This driver already has another schedule during this time.");
            }

            bool vehicleConflict = await _context.Schedules.AnyAsync(s =>
                s.Id != editingScheduleId &&
                s.VehicleId == schedule.VehicleId &&
                s.ScheduleDate.Date == schedule.ScheduleDate.Date &&
                s.Status != "Cancelled" &&
                schedule.DepartureTime < s.ArrivalTime &&
                schedule.ArrivalTime > s.DepartureTime
            );

            if (vehicleConflict)
            {
                ModelState.AddModelError("VehicleId", "This vehicle already has another schedule during this time.");
            }

            bool duplicateRouteDeparture = await _context.Schedules.AnyAsync(s =>
                s.Id != editingScheduleId &&
                s.TransportRouteId == schedule.TransportRouteId &&
                s.ScheduleDate.Date == schedule.ScheduleDate.Date &&
                s.DepartureTime == schedule.DepartureTime &&
                s.Status != "Cancelled"
            );

            if (duplicateRouteDeparture)
            {
                ModelState.AddModelError("DepartureTime", "This route already has a schedule at the same departure time.");
            }
        }

        private void LoadDropdowns(object? selectedRoute = null, object? selectedDriver = null, object? selectedVehicle = null)
        {
            ViewData["TransportRouteId"] = new SelectList(
                _context.TransportRoutes.OrderBy(r => r.RouteName),
                "Id",
                "RouteName",
                selectedRoute
            );

            ViewData["DriverId"] = new SelectList(
                _context.Drivers.OrderBy(d => d.FullName),
                "Id",
                "FullName",
                selectedDriver
            );

            ViewData["VehicleId"] = new SelectList(
                _context.Vehicles.OrderBy(v => v.RegistrationNumber),
                "Id",
                "RegistrationNumber",
                selectedVehicle
            );
        }

        private bool ScheduleExists(int id)
        {
            return _context.Schedules.Any(e => e.Id == id);
        }
    }
}