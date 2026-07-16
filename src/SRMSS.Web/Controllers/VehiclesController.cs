using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SRMSS.Web.Data;
using SRMSS.Web.Filters;
using SRMSS.Web.Models;
using SRMSS.Web.Services;

namespace SRMSS.Web.Controllers
{
    [RoleAuthorize("SuperAdmin", "Admin", "User")]
    public class VehiclesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _auditLogService;

        public VehiclesController(
            ApplicationDbContext context,
            AuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        public async Task<IActionResult> Index(
            string? search,
            string? status,
            string? maintenanceStatus)
        {
            IQueryable<Vehicle> vehicles = _context.Vehicles
                .Include(v => v.Schedules)
                .Include(v => v.FuelLogs)
                .Include(v => v.MaintenanceLogs)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string cleanSearch = search.Trim();

                vehicles = vehicles.Where(v =>
                    v.RegistrationNumber.Contains(cleanSearch) ||
                    v.VehicleType.Contains(cleanSearch) ||
                    (v.Model != null && v.Model.Contains(cleanSearch)) ||
                    (v.Manufacturer != null && v.Manufacturer.Contains(cleanSearch)) ||
                    (v.ChassisNumber != null && v.ChassisNumber.Contains(cleanSearch)) ||
                    v.AssignedDepot.Contains(cleanSearch));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                vehicles = vehicles.Where(v => v.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(maintenanceStatus))
            {
                vehicles = vehicles.Where(v => v.MaintenanceStatus == maintenanceStatus);
            }

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.MaintenanceStatus = maintenanceStatus;
            ViewBag.TotalVehicles = await _context.Vehicles.CountAsync();
            ViewBag.AvailableVehicles = await _context.Vehicles.CountAsync(v => v.Status == "Available" && v.IsActive);
            ViewBag.InServiceVehicles = await _context.Vehicles.CountAsync(v => v.Status == "In Service" && v.IsActive);
            ViewBag.MaintenanceDue = await _context.Vehicles.CountAsync(v => v.MaintenanceStatus == "Due" || v.MaintenanceStatus == "Critical");
            ViewBag.InactiveVehicles = await _context.Vehicles.CountAsync(v => !v.IsActive || v.Status == "Inactive");

            return View(await vehicles
                .OrderByDescending(v => v.IsActive)
                .ThenBy(v => v.RegistrationNumber)
                .ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vehicle = await _context.Vehicles
                .Include(v => v.Schedules)
                    .ThenInclude(s => s.TransportRoute)
                .Include(v => v.Schedules)
                    .ThenInclude(s => s.Driver)
                .Include(v => v.FuelLogs.OrderByDescending(f => f.FuelDate))
                .Include(v => v.MaintenanceLogs.OrderByDescending(m => m.MaintenanceDate))
                .FirstOrDefaultAsync(v => v.Id == id);

            if (vehicle == null)
            {
                return NotFound();
            }

            return View(vehicle);
        }

        [RoleAuthorize("SuperAdmin", "Admin")]
        public IActionResult Create()
        {
            return View(new Vehicle
            {
                Status = "Available",
                MaintenanceStatus = "Good",
                FuelType = "Diesel",
                IsActive = true
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("SuperAdmin", "Admin")]
        public async Task<IActionResult> Create(
            [Bind("Id,RegistrationNumber,VehicleType,Model,Manufacturer,SeatingCapacity,Mileage,FuelType,ChassisNumber,InsuranceExpiryDate,RevenueLicenseExpiryDate,LastServiceDate,NextServiceKm,AssignedDepot,MaintenanceStatus,Status,IsActive")]
            Vehicle vehicle)
        {
            await ValidateVehicle(vehicle);

            if (ModelState.IsValid)
            {
                vehicle.CreatedAt = DateTime.Now;
                vehicle.UpdatedAt = null;

                _context.Vehicles.Add(vehicle);
                await _context.SaveChangesAsync();

                await _auditLogService.LogAsync(
                    "Create Vehicle",
                    "Vehicles",
                    vehicle.Id,
                    $"Created vehicle profile for {vehicle.RegistrationNumber}.");

                TempData["SuccessMessage"] = "Vehicle profile created successfully.";

                return RedirectToAction(nameof(Index));
            }

            return View(vehicle);
        }

        [RoleAuthorize("SuperAdmin", "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vehicle = await _context.Vehicles.FindAsync(id);

            if (vehicle == null)
            {
                return NotFound();
            }

            return View(vehicle);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("SuperAdmin", "Admin")]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,RegistrationNumber,VehicleType,Model,Manufacturer,SeatingCapacity,Mileage,FuelType,ChassisNumber,InsuranceExpiryDate,RevenueLicenseExpiryDate,LastServiceDate,NextServiceKm,AssignedDepot,MaintenanceStatus,Status,IsActive,CreatedAt")]
            Vehicle vehicle)
        {
            if (id != vehicle.Id)
            {
                return NotFound();
            }

            await ValidateVehicle(vehicle, vehicle.Id);

            if (ModelState.IsValid)
            {
                try
                {
                    vehicle.UpdatedAt = DateTime.Now;
                    _context.Update(vehicle);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VehicleExists(vehicle.Id))
                    {
                        return NotFound();
                    }

                    throw;
                }

                await _auditLogService.LogAsync(
                    "Update Vehicle",
                    "Vehicles",
                    vehicle.Id,
                    $"Updated vehicle profile for {vehicle.RegistrationNumber}.");

                TempData["SuccessMessage"] = "Vehicle profile updated successfully.";

                return RedirectToAction(nameof(Index));
            }

            return View(vehicle);
        }

        [RoleAuthorize("SuperAdmin", "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vehicle = await _context.Vehicles
                .Include(v => v.Schedules)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (vehicle == null)
            {
                return NotFound();
            }

            return View(vehicle);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("SuperAdmin", "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);

            if (vehicle == null)
            {
                return NotFound();
            }

            vehicle.IsActive = false;
            vehicle.Status = "Inactive";
            vehicle.MaintenanceStatus = "Unavailable";
            vehicle.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                "Deactivate Vehicle",
                "Vehicles",
                id,
                $"Deactivated vehicle profile for {vehicle.RegistrationNumber}.");

            TempData["SuccessMessage"] = "Vehicle deactivated successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("SuperAdmin", "Admin")]
        public async Task<IActionResult> Reactivate(int id)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);

            if (vehicle == null)
            {
                return NotFound();
            }

            vehicle.IsActive = true;
            vehicle.Status = "Available";
            vehicle.MaintenanceStatus = "Good";
            vehicle.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                "Reactivate Vehicle",
                "Vehicles",
                id,
                $"Reactivated vehicle profile for {vehicle.RegistrationNumber}.");

            TempData["SuccessMessage"] = "Vehicle reactivated successfully.";

            return RedirectToAction(nameof(Index));
        }

        private async Task ValidateVehicle(
            Vehicle vehicle,
            int? editingId = null)
        {
            bool duplicateRegistration = await _context.Vehicles.AnyAsync(v =>
                v.Id != editingId &&
                v.RegistrationNumber == vehicle.RegistrationNumber);

            if (duplicateRegistration)
            {
                ModelState.AddModelError(
                    "RegistrationNumber",
                    "Another vehicle already uses this registration number.");
            }

            if (!string.IsNullOrWhiteSpace(vehicle.ChassisNumber))
            {
                bool duplicateChassis = await _context.Vehicles.AnyAsync(v =>
                    v.Id != editingId &&
                    v.ChassisNumber == vehicle.ChassisNumber);

                if (duplicateChassis)
                {
                    ModelState.AddModelError(
                        "ChassisNumber",
                        "Another vehicle already uses this chassis number.");
                }
            }
        }

        private bool VehicleExists(int id)
        {
            return _context.Vehicles.Any(e => e.Id == id);
        }
    }
}
