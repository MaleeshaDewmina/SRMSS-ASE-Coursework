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

        private static readonly string[] AllowedStatuses =
        {
            "Scheduled",
            "On Time",
            "Delayed",
            "Completed",
            "Cancelled"
        };

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
            ViewBag.RepeatType = "Once";
            ViewBag.RepeatCount = 1;

            LoadDropdowns();
            return View();
        }

        // POST: Schedules/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Id,TransportRouteId,VehicleId,DriverId,ScheduleDate,DepartureTime,ArrivalTime,Status,Notes")] Schedule schedule,
            string repeatType = "Once",
            int repeatCount = 1)
        {
            repeatType = string.IsNullOrWhiteSpace(repeatType) ? "Once" : repeatType;

            if (repeatType != "Once" &&
                repeatType != "Daily" &&
                repeatType != "Weekly" &&
                repeatType != "Monthly")
            {
                ModelState.AddModelError("", "Invalid repeat type selected.");
                repeatType = "Once";
            }

            if (repeatType == "Once")
            {
                repeatCount = 1;
            }

            if (repeatCount < 1)
            {
                ModelState.AddModelError("", "Repeat count must be at least 1.");
            }

            if (repeatCount > 31)
            {
                ModelState.AddModelError("", "Repeat count cannot be more than 31 schedules at once.");
            }

            if (!IsValidStatus(schedule.Status))
            {
                ModelState.AddModelError("Status", "Invalid schedule status selected.");
            }

            var schedulesToCreate = BuildRepeatedSchedules(schedule, repeatType, repeatCount);

            foreach (var repeatedSchedule in schedulesToCreate)
            {
                await ValidateScheduleConflicts(repeatedSchedule);
            }

            if (ModelState.IsValid)
            {
                _context.Schedules.AddRange(schedulesToCreate);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            ViewBag.RepeatType = repeatType;
            ViewBag.RepeatCount = repeatCount;

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

            var schedule = await _context.Schedules
                .Include(s => s.TransportRoute)
                .Include(s => s.Driver)
                .Include(s => s.Vehicle)
                .FirstOrDefaultAsync(s => s.Id == id);

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
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,TransportRouteId,VehicleId,DriverId,ScheduleDate,DepartureTime,ArrivalTime,Status,Notes")] Schedule schedule)
        {
            if (id != schedule.Id)
            {
                return NotFound();
            }

            if (!IsValidStatus(schedule.Status))
            {
                ModelState.AddModelError("Status", "Invalid schedule status selected.");
            }

            await ValidateScheduleConflicts(schedule, schedule.Id);

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Schedules.Update(schedule);
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

            if (!IsValidStatus(status))
            {
                return BadRequest("Invalid schedule status.");
            }

            schedule.Status = status;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // POST: Schedules/UpdateEmergencyAdjustment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateEmergencyAdjustment(int id, string status, string adjustmentNote)
        {
            var schedule = await _context.Schedules.FindAsync(id);

            if (schedule == null)
            {
                return NotFound();
            }

            if (!IsValidStatus(status))
            {
                return BadRequest("Invalid schedule status.");
            }

            schedule.Status = status;

            string cleanNote = string.IsNullOrWhiteSpace(adjustmentNote)
                ? "No reason provided."
                : adjustmentNote.Trim();

            string adjustmentEntry =
                $"[{DateTime.Now:yyyy-MM-dd HH:mm}] Emergency Adjustment: Status changed to {status}. Reason: {cleanNote}";

            if (string.IsNullOrWhiteSpace(schedule.Notes))
            {
                schedule.Notes = adjustmentEntry;
            }
            else
            {
                schedule.Notes = schedule.Notes + Environment.NewLine + adjustmentEntry;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private List<Schedule> BuildRepeatedSchedules(Schedule baseSchedule, string repeatType, int repeatCount)
        {
            var schedules = new List<Schedule>();

            for (int i = 0; i < repeatCount; i++)
            {
                DateTime scheduleDate = baseSchedule.ScheduleDate;

                if (repeatType == "Daily")
                {
                    scheduleDate = baseSchedule.ScheduleDate.AddDays(i);
                }
                else if (repeatType == "Weekly")
                {
                    scheduleDate = baseSchedule.ScheduleDate.AddDays(i * 7);
                }
                else if (repeatType == "Monthly")
                {
                    scheduleDate = baseSchedule.ScheduleDate.AddMonths(i);
                }

                schedules.Add(new Schedule
                {
                    TransportRouteId = baseSchedule.TransportRouteId,
                    VehicleId = baseSchedule.VehicleId,
                    DriverId = baseSchedule.DriverId,
                    ScheduleDate = scheduleDate,
                    DepartureTime = baseSchedule.DepartureTime,
                    ArrivalTime = baseSchedule.ArrivalTime,
                    Status = baseSchedule.Status,
                    Notes = baseSchedule.Notes
                });
            }

            return schedules;
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
            int? selectedRouteId = selectedRoute == null ? null : Convert.ToInt32(selectedRoute);
            int? selectedDriverId = selectedDriver == null ? null : Convert.ToInt32(selectedDriver);
            int? selectedVehicleId = selectedVehicle == null ? null : Convert.ToInt32(selectedVehicle);

            var routeOptions = _context.TransportRoutes
                .Where(r =>
                    r.Status == "Active" ||
                    (selectedRouteId != null && r.Id == selectedRouteId)
                )
                .OrderBy(r => r.RouteName)
                .Select(r => new
                {
                    r.Id,
                    DisplayText = r.RouteName + " | " + r.ServiceType + " | " + r.StartPoint + " to " + r.EndPoint
                })
                .ToList();

            var driverOptions = _context.Drivers
                .Where(d =>
                    d.Status == "Available" ||
                    (selectedDriverId != null && d.Id == selectedDriverId)
                )
                .OrderBy(d => d.FullName)
                .Select(d => new
                {
                    d.Id,
                    DisplayText = d.FullName + " | " + d.Status
                })
                .ToList();

            var vehicleOptions = _context.Vehicles
                .Where(v =>
                    v.Status == "Available" ||
                    (selectedVehicleId != null && v.Id == selectedVehicleId)
                )
                .OrderBy(v => v.RegistrationNumber)
                .Select(v => new
                {
                    v.Id,
                    DisplayText = v.RegistrationNumber + " | " + v.VehicleType + " | " + v.SeatingCapacity + " seats | " + v.Status
                })
                .ToList();

            ViewData["TransportRouteId"] = new SelectList(
                routeOptions,
                "Id",
                "DisplayText",
                selectedRoute
            );

            ViewData["DriverId"] = new SelectList(
                driverOptions,
                "Id",
                "DisplayText",
                selectedDriver
            );

            ViewData["VehicleId"] = new SelectList(
                vehicleOptions,
                "Id",
                "DisplayText",
                selectedVehicle
            );
        }

        private bool IsValidStatus(string status)
        {
            return AllowedStatuses.Contains(status);
        }

        private bool ScheduleExists(int id)
        {
            return _context.Schedules.Any(e => e.Id == id);
        }
    }
}