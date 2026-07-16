using System.ComponentModel.DataAnnotations;

namespace SRMSS.Web.ViewModels
{
    public class UserFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Username is required")]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Role { get; set; } = "User";

        [DataType(DataType.Password)]
        public string? Password { get; set; }

        public bool IsActive { get; set; } = true;
    }
}