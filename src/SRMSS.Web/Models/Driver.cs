using System.ComponentModel.DataAnnotations;

namespace SRMSS.Web.Models
{
    public class Driver
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        [Display(Name = "NIC")]
        public string NIC { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        [Display(Name = "License Number")]
        public string LicenseNumber { get; set; } = string.Empty;

        [Display(Name = "License Expiry Date")]
        public DateTime LicenseExpiryDate { get; set; } = DateTime.Today.AddYears(1);

        [MaxLength(20)]
        [Phone]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(150)]
        [EmailAddress]
        public string? Email { get; set; }

        [MaxLength(250)]
        public string? Address { get; set; }

        [MaxLength(50)]
        [Display(Name = "Employee Number")]
        public string? EmployeeNumber { get; set; }

        [MaxLength(50)]
        [Display(Name = "Assigned Depot")]
        public string AssignedDepot { get; set; } = "Central Depot";

        [MaxLength(30)]
        [Display(Name = "Shift Type")]
        public string ShiftType { get; set; } = "Day";

        [MaxLength(100)]
        [Display(Name = "Emergency Contact Name")]
        public string? EmergencyContactName { get; set; }

        [MaxLength(20)]
        [Display(Name = "Emergency Contact Phone")]
        public string? EmergencyContactPhone { get; set; }

        [Display(Name = "Hire Date")]
        public DateTime HireDate { get; set; } = DateTime.Today;

        [MaxLength(30)]
        public string Status { get; set; } = "Available";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        public ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
    }
}
