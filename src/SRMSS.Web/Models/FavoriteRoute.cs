using System.ComponentModel.DataAnnotations;

namespace SRMSS.Web.Models
{
    public class FavoriteRoute
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string CustomerKey { get; set; } = string.Empty;

        public int TransportRouteId { get; set; }

        public DateTime SavedAt { get; set; } = DateTime.Now;

        public TransportRoute? TransportRoute { get; set; }
    }
}