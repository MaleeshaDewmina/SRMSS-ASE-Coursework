using System.ComponentModel.DataAnnotations;

namespace SRMSS.Web.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        public int? AppUserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Action { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string TableName { get; set; } = string.Empty;

        public int? RecordId { get; set; }

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public AppUser? AppUser { get; set; }
    }
}
