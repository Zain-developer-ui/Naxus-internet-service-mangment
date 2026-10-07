using System.ComponentModel.DataAnnotations;
using NEXUS.Common.Constants;
using NEXUS.Models.ViewModels;

namespace NEXUS.Models.ViewModels
{
    /**
     * The New Connection application. Submitting it does two things at once:
     * the customer account is created and the installation order is raised, so
     * there is no separate registration step and no chance of a customer
     * owning an order they cannot sign in to see.
     */
    public class OrderViewModel
    {
        // ----- Customer -----
        [Required(ErrorMessage = "Full name is required")]
        [Display(Name = "Full name")]
        [MaxLength(120)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "CNIC is required")]
        [Display(Name = "CNIC")]
        [RegularExpression(@"^\d{5}-\d{7}-\d$",
            ErrorMessage = "CNIC must look like 42101-1234567-1")]
        public string Cnic { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mobile number is required")]
        [Display(Name = "Mobile number")]
        [RegularExpression(@"^0\d{3}-\d{7}$",
            ErrorMessage = "Mobile must look like 0300-1234567")]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        [Display(Name = "Email")]
        [MaxLength(120)]
        public string? Email { get; set; }

        // ----- Install address -----
        [Required(ErrorMessage = "Address is required")]
        [Display(Name = "Installation address")]
        [MaxLength(250)]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Select a city")]
        [Display(Name = "City")]
        public int CityId { get; set; }

        // ----- Connection -----
        [Required(ErrorMessage = "Select a connection type")]
        [Display(Name = "Connection type")]
        public ConnectionType ConnectionType { get; set; } = ConnectionType.Broadband;

        [Required(ErrorMessage = "Select a plan")]
        [Display(Name = "Plan")]
        public int PlanId { get; set; }

        [Display(Name = "Billing cycle")]
        public BillingCycle BillingCycle { get; set; } = BillingCycle.Monthly;

        /** Required for Dial-Up and Telephone only; enforced in the service. */
        [Display(Name = "Existing landline number")]
        [RegularExpression(@"^0\d{2}-\d{7}$",
            ErrorMessage = "Landline must look like 021-1234567")]
        public string? LandlineNumber { get; set; }

        [Display(Name = "Corporate account")]
        public bool IsCorporate { get; set; }

        [Display(Name = "Organisation name")]
        [MaxLength(120)]
        public string? OrganisationName { get; set; }

        // ----- Installation preference -----
        [Display(Name = "Preferred date")]
        [DataType(DataType.Date)]
        public DateTime? PreferredInstallDate { get; set; }

        [Display(Name = "Preferred time")]
        public string PreferredTime { get; set; } = string.Empty;

        [Display(Name = "Notes for the technician")]
        [MaxLength(500)]
        public string? Notes { get; set; }

        // ----- Account credentials -----
        [Required(ErrorMessage = "Choose a password")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        [StringLength(200, MinimumLength = 8,
            ErrorMessage = "Password must be at least 8 characters")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm your password")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Display(Name = "I accept the terms of service")]
        [Range(typeof(bool), "true", "true",
            ErrorMessage = "Please accept the terms to continue")]
        public bool AcceptTerms { get; set; }

        // ----- Data shown in the pickers (never posted back) -----
        public IReadOnlyList<CityOption> Cities { get; set; } = Array.Empty<CityOption>();
        public IReadOnlyList<PlanOption> Plans { get; set; } = Array.Empty<PlanOption>();
    }
}
