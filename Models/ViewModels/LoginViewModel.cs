using System.ComponentModel.DataAnnotations;

namespace NEXUS.Models.ViewModels
{
    /**
     * Sign-in form. The identifier accepts either an Account ID or an email
     * address because the SRS lets a customer use whichever they have.
     */
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Account ID or email is required")]
        [Display(Name = "Account ID / Email")]
        [MaxLength(120)]
        public string AccountIdOrEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [MaxLength(200)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Keep me signed in")]
        public bool RememberMe { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Current password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        [StringLength(200, MinimumLength = 8,
            ErrorMessage = "Password must be at least 8 characters")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm the new password")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
