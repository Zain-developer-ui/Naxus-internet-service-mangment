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
}
