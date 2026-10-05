using System.ComponentModel.DataAnnotations;

namespace NEXUS.Models.ViewModels
{
    /// <summary>
    /// Data captured by the multi-step New Connection form.
    /// All attributes drive frontend validation.
    /// </summary>
    public class OrderViewModel
    {
        // ----- Step 1: Customer Info -----
        [Required(ErrorMessage = "Full name is required")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "CNIC is required")]
        [RegularExpression(@"^\d{5}-\d{7}-\d$", ErrorMessage = "Format: 35202-1234567-1")]
        public string Cnic { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mobile number is required")]
        [RegularExpression(@"^03\d{9}$", ErrorMessage = "Format: 03001234567")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Enter a valid email")]
        public string Email { get; set; } = string.Empty;

        // ----- Step 2: Address -----
        [Required(ErrorMessage = "Address is required")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Area is required")]
        public string Area { get; set; } = string.Empty;

        [Required(ErrorMessage = "Postal code is required")]
        public string PostalCode { get; set; } = string.Empty;

        // ----- Step 3: Connection -----
        [Required(ErrorMessage = "Please select a connection type")]
        public string ServiceType { get; set; } = string.Empty;   // Dial-Up / Broadband

        [Required(ErrorMessage = "Please select a plan")]
        public string PlanName { get; set; } = string.Empty;      // Basic / Standard / Premium

        [Required(ErrorMessage = "Please select a speed")]
        public string Speed { get; set; } = string.Empty;

        // ----- Step 4: Installation -----
        [DataType(DataType.Date)]
        public DateTime? PreferredInstallDate { get; set; }

        public string PreferredTime { get; set; } = string.Empty; // Morning / Afternoon / Evening
        public string? Notes { get; set; }

        // ----- Step 5: Review -----
        public decimal EstimatedTotal { get; set; }

        // ----- Step 6: Terms -----
        [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept the terms")]
        public bool AcceptTerms { get; set; }

        // ----- File uploads (frontend only) -----
        public string? CnicDocName { get; set; }
        public string? AddressDocName { get; set; }
    }
}