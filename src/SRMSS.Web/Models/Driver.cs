using System.ComponentModel.DataAnnotations;

namespace SRMSS.Web.Models
{
    public class Driver
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string NIC { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string LicenseNumber { get; set; } = string.Empty;

        public DateTime LicenseExpiryDate { get; set; }

        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(30)]
        public string Status { get; set; } = "Available";

        public ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
    }
}
