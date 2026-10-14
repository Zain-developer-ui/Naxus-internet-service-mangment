using System.ComponentModel.DataAnnotations;

namespace NEXUS.Models.ViewModels
{
    public class ProfileViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string AccountId { get; set; } = string.Empty;
        public string MemberSince { get; set; } = string.Empty;

        /** Human readable role, e.g. "Administrator" - shown on the identity card. */
        public string RoleLabel { get; set; } = string.Empty;

        /** True for Admin/Accounts/Technical/Retail. Hides every customer-only block. */
        public bool IsStaff { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime? LastLoginAt { get; set; }

        // Customer-only fields. Left empty for staff.
        public string Cnic { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string CityName { get; set; } = string.Empty;
        public string ConnectionTypeName { get; set; } = string.Empty;
        public string LandlineNumber { get; set; } = string.Empty;
        public bool IsCorporate { get; set; }
        public string OrganisationName { get; set; } = string.Empty;
        public int ConnectionCount { get; set; }

        // Staff-only fields.
        public string Designation { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public DateTime? JoinedOn { get; set; }

        public List<DocumentViewModel> Documents { get; set; } = new();
    }

    public class DocumentViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string UploadedOn { get; set; } = string.Empty;
    }

    /**
     * What a signed-in user may change about themselves. Email, account id and
     * CNIC are deliberately absent: those are identity fields, and letting a
     * user rewrite them would break the records that reference them.
     */
    public class ProfileEditViewModel
    {
        [Required(ErrorMessage = "Enter your full name.")]
        [StringLength(120, ErrorMessage = "Keep the name under 120 characters.")]
        [Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter a contact number.")]
        [StringLength(30, ErrorMessage = "Keep the number under 30 characters.")]
        [Display(Name = "Contact number")]
        public string Phone { get; set; } = string.Empty;

        /** Customers only. Staff have no address on file, so the field is hidden. */
        [StringLength(250, ErrorMessage = "Keep the address under 250 characters.")]
        [Display(Name = "Address")]
        public string? Address { get; set; }

        // Read-only context, echoed back so the form can explain itself.
        public string AccountId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string RoleLabel { get; set; } = string.Empty;
        public bool IsStaff { get; set; }
    }
}
