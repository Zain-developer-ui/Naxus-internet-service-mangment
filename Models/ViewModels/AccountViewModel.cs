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

        /** The plan the connection is on, so the page can link to its own record. */
        public int? PlanId { get; set; }

        /** Built from the plan's own columns rather than a fixed marketing list. */
        public IReadOnlyList<string> PlanFeatures { get; set; } = Array.Empty<string>();

        /** The real order-to-activation journey, read from the status history. */
        public IReadOnlyList<AccountTimelineEntry> Timeline { get; set; } = Array.Empty<AccountTimelineEntry>();

        /** Equipment NEXUS issues, read from the catalogue. */
        public IReadOnlyList<AccountEquipmentRow> Equipment { get; set; } = Array.Empty<AccountEquipmentRow>();

        public bool HasPlanFeatures => PlanFeatures.Count > 0;
        public bool HasTimeline => Timeline.Count > 0;
        public bool HasEquipment => Equipment.Count > 0;
    }

    public sealed record AccountTimelineEntry(string When, string Title, string Detail, bool Done);

    public sealed record AccountEquipmentRow(string Name, string Description);
}