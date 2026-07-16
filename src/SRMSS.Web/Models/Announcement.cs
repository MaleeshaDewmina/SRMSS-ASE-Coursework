using System.ComponentModel.DataAnnotations;

namespace SRMSS.Web.Models
{
    public class Announcement
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(1500)]
        public string Message { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        public string Audience { get; set; } = "All";

        [Required]
        [MaxLength(30)]
        public string Priority { get; set; } = "Normal";

        [MaxLength(30)]
        public string Status { get; set; } = "Published";

        [Display(Name = "Publish Date")]
        public DateTime PublishDate { get; set; } = DateTime.Today;

        [Display(Name = "Expiry Date")]
        public DateTime? ExpiryDate { get; set; }

        [MaxLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }
    }
}
