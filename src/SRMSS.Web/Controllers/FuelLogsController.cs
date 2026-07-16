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
    public class FuelLogsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _auditLogService;

        public FuelLogsController(
            ApplicationDbContext context,
            AuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        public async Task<IActionResult> Index(
            int? vehicleId,
            DateTime? fromDate,
            DateTime? toDate,
            string? search)
        {
            IQueryable<FuelLog> logs = _context.FuelLogs
                .Include(f => f.Vehicle)
                .AsQueryable();

            if (vehicleId.HasValue)
            {
                logs = logs.Where(f => f.VehicleId == vehicleId.Value);
            }

            if (fromDate.HasValue)
            {
                logs = logs.Where(f => f.FuelDate.Date >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                logs = logs.Where(f => f.FuelDate.Date <= toDate.Value.Date);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string cleanSearch = search.Trim();

                logs = logs.Where(f =>
                    (f.Vehicle != null && f.Vehicle.RegistrationNumber.Contains(cleanSearch)) ||
                    (f.FuelStation != null && f.FuelStation.Contains(cleanSearch)) ||
                    (f.ReceiptNumber != null && f.ReceiptNumber.Contains(cleanSearch)));
            }

            ViewBag.VehicleId = vehicleId;
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;
            ViewBag.Search = search;
            await LoadVehicleDropdown(vehicleId);

            ViewBag.TotalFuelLogs = await _context.FuelLogs.CountAsync();
            ViewBag.MonthlyFuelCost = await _context.FuelLogs
                .Where(f => f.FuelDate.Month == DateTime.Today.Month && f.FuelDate.Year == DateTime.Today.Year)
                .SumAsync(f => (decimal?)f.Cost) ?? 0;
            ViewBag.MonthlyLitres = await _context.FuelLogs
                .Where(f => f.FuelDate.Month == DateTime.Today.Month && f.FuelDate.Year == DateTime.Today.Year)
                .SumAsync(f => (decimal?)f.Litres) ?? 0;
            ViewBag.AverageEfficiency = await _context.FuelLogs
                .Where(f => f.FuelEfficiencyKmPerLitre > 0)
                .AverageAsync(f => (decimal?)f.FuelEfficiencyKmPerLitre) ?? 0;

            return View(await logs
                .OrderByDescending(f => f.FuelDate)
                .ThenByDescending(f => f.Id)
                .ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var fuelLog = await _context.FuelLogs
                .Include(f => f.Vehicle)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (fuelLog == null)
            {
                return NotFound();
            }

            return View(fuelLog);
        }

        public async Task<IActionResult> Create(int? vehicleId)
        {
            await LoadVehicleDropdown(vehicleId);

            return View(new FuelLog
            {
                VehicleId = vehicleId ?? 0,
                FuelDate = DateTime.Today
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Id,VehicleId,FuelDate,Litres,Cost,DistanceCoveredKm,FuelEfficiencyKmPerLitre,OdometerReading,FuelStation,ReceiptNumber,Notes")]
            FuelLog fuelLog)
        {
            ValidateFuelLog(fuelLog);

            if (ModelState.IsValid)
            {
                fuelLog.FuelEfficiencyKmPerLitre = CalculateEfficiency(fuelLog);
                fuelLog.CreatedAt = DateTime.Now;

                _context.FuelLogs.Add(fuelLog);
                await UpdateVehicleMileage(fuelLog.VehicleId, fuelLog.OdometerReading);
                await _context.SaveChangesAsync();

                await _auditLogService.LogAsync(
                    "Create Fuel Log",
                    "FuelLogs",
                    fuelLog.Id,
                    $"Recorded {fuelLog.Litres:N2} litres for vehicle #{fuelLog.VehicleId}.");

                TempData["SuccessMessage"] = "Fuel log recorded successfully.";

                return RedirectToAction(nameof(Index));
            }

            await LoadVehicleDropdown(fuelLog.VehicleId);
            return View(fuelLog);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var fuelLog = await _context.FuelLogs.FindAsync(id);

            if (fuelLog == null)
            {
                return NotFound();
            }

            await LoadVehicleDropdown(fuelLog.VehicleId);
            return View(fuelLog);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,VehicleId,FuelDate,Litres,Cost,DistanceCoveredKm,FuelEfficiencyKmPerLitre,OdometerReading,FuelStation,ReceiptNumber,Notes,CreatedAt")]
            FuelLog fuelLog)
        {
            if (id != fuelLog.Id)
            {
                return NotFound();
            }

            ValidateFuelLog(fuelLog);

            if (ModelState.IsValid)
            {
                try
                {
                    fuelLog.FuelEfficiencyKmPerLitre = CalculateEfficiency(fuelLog);
                    _context.Update(fuelLog);
                    await UpdateVehicleMileage(fuelLog.VehicleId, fuelLog.OdometerReading);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FuelLogExists(fuelLog.Id))
                    {
                        return NotFound();
                    }

                    throw;
                }

                await _auditLogService.LogAsync(
                    "Update Fuel Log",
                    "FuelLogs",
                    fuelLog.Id,
                    $"Updated fuel log #{fuelLog.Id}.");

                TempData["SuccessMessage"] = "Fuel log updated successfully.";

                return RedirectToAction(nameof(Index));
            }

            await LoadVehicleDropdown(fuelLog.VehicleId);
            return View(fuelLog);
        }

        [RoleAuthorize("SuperAdmin", "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var fuelLog = await _context.FuelLogs
                .Include(f => f.Vehicle)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (fuelLog == null)
            {
                return NotFound();
            }

            return View(fuelLog);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("SuperAdmin", "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var fuelLog = await _context.FuelLogs.FindAsync(id);

            if (fuelLog == null)
            {
                return NotFound();
            }

            _context.FuelLogs.Remove(fuelLog);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                "Delete Fuel Log",
                "FuelLogs",
                id,
                $"Deleted fuel log #{id}.");

            TempData["SuccessMessage"] = "Fuel log deleted successfully.";

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
                    DisplayText = v.RegistrationNumber + " | " + v.VehicleType + " | " + v.Status
                })
                .ToListAsync();

            ViewData["VehicleId"] = new SelectList(vehicles, "Id", "DisplayText", selectedVehicle);
        }

        private void ValidateFuelLog(FuelLog fuelLog)
        {
            if (fuelLog.VehicleId <= 0)
            {
                ModelState.AddModelError("VehicleId", "Please select a vehicle.");
            }

            if (fuelLog.Litres <= 0)
            {
                ModelState.AddModelError("Litres", "Litres must be greater than zero.");
            }

            if (fuelLog.Cost <= 0)
            {
                ModelState.AddModelError("Cost", "Cost must be greater than zero.");
            }
        }

        private static decimal CalculateEfficiency(FuelLog fuelLog)
        {
            if (fuelLog.Litres <= 0 || fuelLog.DistanceCoveredKm <= 0)
            {
                return 0;
            }

            return Math.Round(fuelLog.DistanceCoveredKm / fuelLog.Litres, 2);
        }

        private async Task UpdateVehicleMileage(int vehicleId, int odometerReading)
        {
            var vehicle = await _context.Vehicles.FindAsync(vehicleId);

            if (vehicle != null && odometerReading > vehicle.Mileage)
            {
                vehicle.Mileage = odometerReading;
                vehicle.UpdatedAt = DateTime.Now;
            }
        }

        private bool FuelLogExists(int id)
        {
            return _context.FuelLogs.Any(e => e.Id == id);
        }
    }
}
