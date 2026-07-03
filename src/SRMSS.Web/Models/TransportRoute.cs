using System.ComponentModel.DataAnnotations;

namespace SRMSS.Web.Models
{
    public class TransportRoute
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string RouteName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string StartPoint { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string EndPoint { get; set; } = string.Empty;

        public decimal DistanceKm { get; set; }

        [MaxLength(50)]
        public string ServiceType { get; set; } = "Normal";

        [MaxLength(30)]
        public string Status { get; set; } = "Active";

        public ICollection<RouteStop> RouteStops { get; set; } = new List<RouteStop>();
        public ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
    }
}
