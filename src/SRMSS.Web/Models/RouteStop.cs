using System.ComponentModel.DataAnnotations;

namespace SRMSS.Web.Models
{
    public class RouteStop
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Route")]
        public int TransportRouteId { get; set; }

        public TransportRoute? TransportRoute { get; set; }

        [Required(ErrorMessage = "Stop name is required")]
        [MaxLength(100)]
        [Display(Name = "Stop Name")]
        public string StopName { get; set; } = string.Empty;

        [Required]
        [Range(1, 100, ErrorMessage = "Stop order must be between 1 and 100")]
        [Display(Name = "Stop Order")]
        public int StopOrder { get; set; }

        [Range(0, 1440, ErrorMessage = "Estimated minutes must be between 0 and 1440")]
        [Display(Name = "Estimated Minutes From Start")]
        public int EstimatedMinutesFromStart { get; set; }
    }
}