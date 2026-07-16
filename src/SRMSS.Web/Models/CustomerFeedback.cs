using System.ComponentModel.DataAnnotations;

namespace SRMSS.Web.Models
{
    public class CustomerFeedback
    {
        public int Id { get; set; }

        [MaxLength(100)]
        public string CustomerKey { get; set; } = string.Empty;

        public int? AppUserId { get; set; }

        public int? TransportRouteId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Message { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = "General";

        [Range(1, 5)]
        public int Rating { get; set; } = 5;

        [MaxLength(30)]
        public string Status { get; set; } = "Open";

        [MaxLength(30)]
        public string Priority { get; set; } = "Normal";

        [MaxLength(1000)]
        public string? Response { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.Now;

        public DateTime? RespondedAt { get; set; }

        public AppUser? AppUser { get; set; }

        public TransportRoute? TransportRoute { get; set; }
    }
}
