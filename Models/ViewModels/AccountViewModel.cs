namespace NEXUS.Models.ViewModels
{
    /// <summary>
    /// Customer account data — used for login redirect, My Connection,
    /// and the Account Status public search.
    /// </summary>
    public class AccountViewModel
    {
        public string AccountId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Cnic { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        public string ConnectionType { get; set; } = string.Empty;
        public string Plan { get; set; } = string.Empty;
        public string Speed { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;          // Active / Suspended / Pending
        public string BillingStatus { get; set; } = string.Empty;   // Paid / Due
        public string InstallationStatus { get; set; } = string.Empty;

        public DateTime? InstallationDate { get; set; }
        public DateTime? ActivationDate { get; set; }

        public string RouterModel { get; set; } = string.Empty;
        public string ServiceArea { get; set; } = string.Empty;
        public string TechnicianName { get; set; } = string.Empty;
    }
}