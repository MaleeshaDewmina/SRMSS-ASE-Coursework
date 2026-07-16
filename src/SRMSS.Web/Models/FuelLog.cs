using System.ComponentModel.DataAnnotations;

namespace SRMSS.Web.Models
{
    public class FuelLog
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Vehicle")]
        public int VehicleId { get; set; }

        [Display(Name = "Fuel Date")]
        public DateTime FuelDate { get; set; } = DateTime.Today;

        [Range(0.01, 10000)]
        public decimal Litres { get; set; }

        [Range(0.01, 999999999)]
        public decimal Cost { get; set; }

        [Display(Name = "Distance Covered")]
        [Range(0, 999999)]
        public decimal DistanceCoveredKm { get; set; }

        [Display(Name = "Fuel Efficiency")]
        public decimal FuelEfficiencyKmPerLitre { get; set; }

        [Display(Name = "Odometer Reading")]
        [Range(0, 9999999)]
        public int OdometerReading { get; set; }

        [MaxLength(120)]
        [Display(Name = "Fuel Station")]
        public string? FuelStation { get; set; }

        [MaxLength(80)]
        [Display(Name = "Receipt Number")]
        public string? ReceiptNumber { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public Vehicle? Vehicle { get; set; }
    }
}
