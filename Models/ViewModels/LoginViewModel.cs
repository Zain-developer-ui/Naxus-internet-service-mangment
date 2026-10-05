using System.ComponentModel.DataAnnotations;

namespace NEXUS.Models.ViewModels
{
    /// <summary>
    /// Customer login form. Frontend validation only — the backend
    /// developer will wire real authentication later.
    /// </summary>
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Account ID or Email is required")]
        [Display(Name = "Account ID / Email")]
        public string AccountIdOrEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }
}