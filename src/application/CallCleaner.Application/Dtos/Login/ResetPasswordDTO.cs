using System.ComponentModel.DataAnnotations;

namespace CallCleaner.Application.Dtos.Login
{
    public class ResetPasswordDTO
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Code is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Code must be 6 characters")]
        public string Code { get; set; }

        [Required(ErrorMessage = "New password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string NewPassword { get; set; }
    }
}
