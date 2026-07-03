using System.ComponentModel.DataAnnotations;

namespace SRMSS.Web.Models
{
    public class MaintenanceLog
    {
        public int Id { get; set; }

        public int VehicleId { get; set; }

        public DateTime MaintenanceDate { get; set; }

        [Required]
        [MaxLength(100)]
        public string MaintenanceType { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public decimal Cost { get; set; }

        public DateTime? NextServiceDate { get; set; }

        public Vehicle? Vehicle { get; set; }
    }
}
