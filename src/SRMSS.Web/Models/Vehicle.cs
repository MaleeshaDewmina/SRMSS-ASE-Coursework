using System.ComponentModel.DataAnnotations;

namespace SRMSS.Web.Models
{
    public class Vehicle
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(30)]
        [Display(Name = "Registration Number")]
        public string RegistrationNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        [Display(Name = "Vehicle Type")]
        public string VehicleType { get; set; } = string.Empty;

        [MaxLength(80)]
        public string? Model { get; set; }

        [MaxLength(80)]
        public string? Manufacturer { get; set; }

        [Display(Name = "Seating Capacity")]
        [Range(1, 120)]
        public int SeatingCapacity { get; set; }

        [Display(Name = "Current Mileage")]
        [Range(0, 9999999)]
        public decimal Mileage { get; set; }

        [MaxLength(30)]
        [Display(Name = "Fuel Type")]
        public string FuelType { get; set; } = "Diesel";

        [MaxLength(80)]
        [Display(Name = "Chassis Number")]
        public string? ChassisNumber { get; set; }

        [Display(Name = "Insurance Expiry")]
        public DateTime? InsuranceExpiryDate { get; set; }

        [Display(Name = "Revenue License Expiry")]
        public DateTime? RevenueLicenseExpiryDate { get; set; }

        [Display(Name = "Last Service Date")]
        public DateTime? LastServiceDate { get; set; }

        [Display(Name = "Next Service Mileage")]
        public int? NextServiceKm { get; set; }

        [MaxLength(50)]
        [Display(Name = "Assigned Depot")]
        public string AssignedDepot { get; set; } = "Central Depot";

        [MaxLength(30)]
        [Display(Name = "Maintenance Status")]
        public string MaintenanceStatus { get; set; } = "Good";

        [MaxLength(30)]
        public string Status { get; set; } = "Available";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        public ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
        public ICollection<FuelLog> FuelLogs { get; set; } = new List<FuelLog>();
        public ICollection<MaintenanceLog> MaintenanceLogs { get; set; } = new List<MaintenanceLog>();
    }
}
