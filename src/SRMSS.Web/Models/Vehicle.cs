using System.ComponentModel.DataAnnotations;

namespace SRMSS.Web.Models
{
    public class Vehicle
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(30)]
        public string RegistrationNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string VehicleType { get; set; } = string.Empty;

        public int SeatingCapacity { get; set; }

        public decimal Mileage { get; set; }

        [MaxLength(30)]
        public string Status { get; set; } = "Available";

        public ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
        public ICollection<FuelLog> FuelLogs { get; set; } = new List<FuelLog>();
        public ICollection<MaintenanceLog> MaintenanceLogs { get; set; } = new List<MaintenanceLog>();
    }
}
