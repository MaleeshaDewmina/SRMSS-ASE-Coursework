using System.ComponentModel.DataAnnotations;

namespace SRMSS.Web.Models
{
    public class RouteStop
    {
        public int Id { get; set; }

        public int TransportRouteId { get; set; }

        public TransportRoute? TransportRoute { get; set; }

        [Required]
        [MaxLength(100)]
        public string StopName { get; set; } = string.Empty;

        public int StopOrder { get; set; }
    }
}
