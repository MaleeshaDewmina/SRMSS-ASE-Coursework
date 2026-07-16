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
    public class MaintenanceLogsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _auditLogService;

        public MaintenanceLogsController(
            ApplicationDbContext context,
            AuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        public async Task<IActionResult> Index(
            int? vehicleId,
            string? status,
            string? type,
            DateTime? fromDate,
            DateTime? toDate)
        {
            IQueryable<MaintenanceLog> logs = _context.MaintenanceLogs
                .Include(m => m.Vehicle)
                .AsQueryable();

            if (vehicleId.HasValue)
            {
                logs = logs.Where(m => m.VehicleId == vehicleId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                logs = logs.Where(m => m.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                logs = logs.Where(m => m.MaintenanceType.Contains(type));
            }

            if (fromDate.HasValue)
            {
                logs = logs.Where(m => m.MaintenanceDate.Date >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                logs = logs.Where(m => m.MaintenanceDate.Date <= toDate.Value.Date);
            }

            ViewBag.VehicleId = vehicleId;
            ViewBag.Status = status;
            ViewBag.Type = type;
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;
            await LoadVehicleDropdown(vehicleId);

            ViewBag.TotalMaintenance = await _context.MaintenanceLogs.CountAsync();
            ViewBag.MonthlyMaintenanceCost = await _context.MaintenanceLogs
                .Where(m => m.MaintenanceDate.Month == DateTime.Today.Month && m.MaintenanceDate.Year == DateTime.Today.Year)
                .SumAsync(m => (decimal?)m.Cost) ?? 0;
            ViewBag.UpcomingServices = await _context.MaintenanceLogs
                .CountAsync(m => m.NextServiceDate != null && m.NextServiceDate >= DateTime.Today && m.NextServiceDate <= DateTime.Today.AddDays(30));
            ViewBag.OverdueServices = await _context.MaintenanceLogs
                .CountAsync(m => m.NextServiceDate != null && m.NextServiceDate < DateTime.Today);

            return View(await logs
                .OrderByDescending(m => m.MaintenanceDate)
                .ThenByDescending(m => m.Id)
                .ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var maintenanceLog = await _context.MaintenanceLogs
                .Include(m => m.Vehicle)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (maintenanceLog == null)
            {
                return NotFound();
            }

            return View(maintenanceLog);
        }

        public async Task<IActionResult> Create(int? vehicleId)
        {
            await LoadVehicleDropdown(vehicleId);

            return View(new MaintenanceLog
            {
                VehicleId = vehicleId ?? 0,
                MaintenanceDate = DateTime.Today,
                Status = "Completed"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Id,VehicleId,MaintenanceDate,MaintenanceType,Description,Cost,OdometerReading,PerformedBy,Status,NextServiceDate")]
            MaintenanceLog maintenanceLog)
        {
            ValidateMaintenanceLog(maintenanceLog);

            if (ModelState.IsValid)
            {
                maintenanceLog.CreatedAt = DateTime.Now;
                _context.MaintenanceLogs.Add(maintenanceLog);

                await UpdateVehicleMaintenanceState(maintenanceLog);
                await _context.SaveChangesAsync();

                await _auditLogService.LogAsync(
                    "Create Maintenance Log",
                    "MaintenanceLogs",
                    maintenanceLog.Id,
                    $"Recorded {maintenanceLog.MaintenanceType} maintenance for vehicle #{maintenanceLog.VehicleId}.");

                TempData["SuccessMessage"] = "Maintenance record saved successfully.";

                return RedirectToAction(nameof(Index));
            }

            await LoadVehicleDropdown(maintenanceLog.VehicleId);
            return View(maintenanceLog);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var maintenanceLog = await _context.MaintenanceLogs.FindAsync(id);

            if (maintenanceLog == null)
            {
                return NotFound();
            }

            await LoadVehicleDropdown(maintenanceLog.VehicleId);
            return View(maintenanceLog);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,VehicleId,MaintenanceDate,MaintenanceType,Description,Cost,OdometerReading,PerformedBy,Status,NextServiceDate,CreatedAt")]
            MaintenanceLog maintenanceLog)
        {
            if (id != maintenanceLog.Id)
            {
                return NotFound();
            }

            ValidateMaintenanceLog(maintenanceLog);

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(maintenanceLog);
                    await UpdateVehicleMaintenanceState(maintenanceLog);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MaintenanceLogExists(maintenanceLog.Id))
                    {
                        return NotFound();
                    }

                    throw;
                }

                await _auditLogService.LogAsync(
                    "Update Maintenance Log",
                    "MaintenanceLogs",
                    maintenanceLog.Id,
                    $"Updated maintenance log #{maintenanceLog.Id}.");

                TempData["SuccessMessage"] = "Maintenance record updated successfully.";

                return RedirectToAction(nameof(Index));
            }

            await LoadVehicleDropdown(maintenanceLog.VehicleId);
            return View(maintenanceLog);
        }

        [RoleAuthorize("SuperAdmin", "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var maintenanceLog = await _context.MaintenanceLogs
                .Include(m => m.Vehicle)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (maintenanceLog == null)
            {
                return NotFound();
            }

            return View(maintenanceLog);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("SuperAdmin", "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var maintenanceLog = await _context.MaintenanceLogs.FindAsync(id);

            if (maintenanceLog == null)
            {
                return NotFound();
            }

            _context.MaintenanceLogs.Remove(maintenanceLog);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                "Delete Maintenance Log",
                "MaintenanceLogs",
                id,
                $"Deleted maintenance log #{id}.");

            TempData["SuccessMessage"] = "Maintenance record deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadVehicleDropdown(object? selectedVehicle = null)
        {
            var vehicles = await _context.Vehicles
                .Where(v => v.IsActive || (selectedVehicle != null && v.Id == Convert.ToInt32(selectedVehicle)))
                .OrderBy(v => v.RegistrationNumber)
                .Select(v => new
                {
                    v.Id,
                    DisplayText = v.RegistrationNumber + " | " + v.VehicleType + " | " + v.MaintenanceStatus
                })
                .ToListAsync();

            ViewData["VehicleId"] = new SelectList(vehicles, "Id", "DisplayText", selectedVehicle);
        }

        private void ValidateMaintenanceLog(MaintenanceLog maintenanceLog)
        {
            if (maintenanceLog.VehicleId <= 0)
            {
                ModelState.AddModelError("VehicleId", "Please select a vehicle.");
            }

            if (string.IsNullOrWhiteSpace(maintenanceLog.MaintenanceType))
            {
                ModelState.AddModelError("MaintenanceType", "Maintenance type is required.");
            }
        }

        private async Task UpdateVehicleMaintenanceState(MaintenanceLog maintenanceLog)
        {
            var vehicle = await _context.Vehicles.FindAsync(maintenanceLog.VehicleId);

            if (vehicle == null)
            {
                return;
            }

            vehicle.LastServiceDate = maintenanceLog.MaintenanceDate;
            vehicle.UpdatedAt = DateTime.Now;

            if (maintenanceLog.NextServiceDate != null && maintenanceLog.NextServiceDate < DateTime.Today)
            {
                vehicle.MaintenanceStatus = "Due";
            }
            else if (maintenanceLog.Status == "In Progress")
            {
                vehicle.MaintenanceStatus = "In Maintenance";
                vehicle.Status = "Maintenance";
            }
            else if (maintenanceLog.Status == "Completed")
            {
                vehicle.MaintenanceStatus = "Good";
                if (vehicle.Status == "Maintenance")
                {
                    vehicle.Status = "Available";
                }
            }
        }

        private bool MaintenanceLogExists(int id)
        {
            return _context.MaintenanceLogs.Any(e => e.Id == id);
        }
    }
}
