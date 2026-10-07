using System.ComponentModel.DataAnnotations;

namespace NEXUS.Models.ViewModels
{
    /**
     * Read-only snapshot of an order for the public tracking page.
     * Separate from OrderViewModel because tracking shows an order that has
     * already been placed - there is nothing to edit and no credentials.
     */
    public class OrderTrackingViewModel
    {
        public string OrderId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string ConnectionType { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
        public string StatusBadge { get; set; } = "badge-neutral";

        public DateTime? ScheduledFor { get; set; }
        public DateTime PlacedAt { get; set; }

        public decimal MonthlyRate { get; set; }
        public decimal SecurityDeposit { get; set; }

        public IReadOnlyList<TrackingStep> Timeline { get; set; } = Array.Empty<TrackingStep>();
    }

    public sealed record TrackingStep(string Title, string Description, string State);
}
