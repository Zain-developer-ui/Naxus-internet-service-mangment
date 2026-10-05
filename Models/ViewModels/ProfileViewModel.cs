namespace NEXUS.Models.ViewModels
{
    public class ProfileViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Cnic { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string AccountId { get; set; } = string.Empty;
        public string MemberSince { get; set; } = string.Empty;

        public List<DocumentViewModel> Documents { get; set; } = new();

        // Notification preferences (checkbox state)
        public bool EmailNotifications { get; set; } = true;
        public bool SmsNotifications { get; set; } = true;
        public bool PromoNotifications { get; set; } = false;
    }

    public class DocumentViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string UploadedOn { get; set; } = string.Empty;
    }
}