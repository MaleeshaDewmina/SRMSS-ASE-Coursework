using System.ComponentModel.DataAnnotations;

namespace SRMSS.Web.Models
{
    public class MaintenanceLog
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Vehicle")]
        public int VehicleId { get; set; }

        [Display(Name = "Maintenance Date")]
        public DateTime MaintenanceDate { get; set; } = DateTime.Today;

        [Required]
        [MaxLength(100)]
        [Display(Name = "Maintenance Type")]
        public string MaintenanceType { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Range(0, 999999999)]
        public decimal Cost { get; set; }

        [Display(Name = "Odometer Reading")]
        public int? OdometerReading { get; set; }

        [MaxLength(100)]
        [Display(Name = "Performed By")]
        public string? PerformedBy { get; set; }

        [MaxLength(30)]
        public string Status { get; set; } = "Completed";

        [Display(Name = "Next Service Date")]
        public DateTime? NextServiceDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public Vehicle? Vehicle { get; set; }
    }
}
