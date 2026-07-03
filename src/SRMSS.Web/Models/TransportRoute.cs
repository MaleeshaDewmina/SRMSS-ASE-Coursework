using System.ComponentModel.DataAnnotations;

namespace SRMSS.Web.Models
{
    public class TransportRoute
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Route name is required")]
        [MaxLength(100)]
        [Display(Name = "Route Name")]
        public string RouteName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Start point is required")]
        [MaxLength(100)]
        [Display(Name = "Start Point")]
        public string StartPoint { get; set; } = string.Empty;

        [Required(ErrorMessage = "End point is required")]
        [MaxLength(100)]
        [Display(Name = "End Point")]
        public string EndPoint { get; set; } = string.Empty;

        [Required]
        [Range(1, 1000, ErrorMessage = "Distance must be between 1 and 1000 km")]
        [Display(Name = "Distance (Km)")]
        public decimal DistanceKm { get; set; }

        [Required]
        [Range(1, 1440, ErrorMessage = "Duration must be between 1 and 1440 minutes")]
        [Display(Name = "Estimated Duration (Minutes)")]
        public int EstimatedDurationMinutes { get; set; }

        [Required]
        [MaxLength(50)]
        [Display(Name = "Service Type")]
        public string ServiceType { get; set; } = "Normal";

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "Active";

        public ICollection<RouteStop> RouteStops { get; set; } = new List<RouteStop>();
        public ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
    }
}