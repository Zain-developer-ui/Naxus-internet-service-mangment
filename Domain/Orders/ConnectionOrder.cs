using System.ComponentModel.DataAnnotations;
using NEXUS.Common.Constants;
using NEXUS.Domain.Catalog;
using NEXUS.Domain.Customers;
using NEXUS.Domain.Organisation;

namespace NEXUS.Domain.Orders;

public class ConnectionOrder : AuditableEntity
{
    public int Id { get; set; }

    /** SRS format: D/T/B prefix + 10 digit serial. */
    [Required, MaxLength(11)]
    public string OrderId { get; set; } = string.Empty;

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int PlanId { get; set; }
    public Plan Plan { get; set; } = null!;

    public ConnectionType ConnectionType { get; set; }

    public BillingCycle BillingCycle { get; set; } = BillingCycle.Monthly;

    public OrderStatus Status { get; set; } = OrderStatus.AwaitingFeasibility;

    [MaxLength(250)]
    public string InstallationAddress { get; set; } = string.Empty;

    public int CityId { get; set; }
    public City City { get; set; } = null!;

    [MaxLength(20)]
    public string? LandlineNumber { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public decimal QuotedAmount { get; set; }

    public int? AssignedTechnicianId { get; set; }
    public Employee? AssignedTechnician { get; set; }

    public DateTime? ScheduledFor { get; set; }

    /** Free-text slot the customer asked for, e.g. "Morning (9am–12pm)". */
    [MaxLength(40)]
    public string? PreferredSlot { get; set; }

    public DateTime? CompletedAt { get; set; }

    public int? CreatedByShopId { get; set; }
    public RetailShop? CreatedByShop { get; set; }

    public FeasibilityCheck? Feasibility { get; set; }
    public Connection? Connection { get; set; }

    public bool AwaitingFeasibility => Status == OrderStatus.AwaitingFeasibility;

    public bool CanBeConfirmed =>
        Status is OrderStatus.FeasibilityPassed or OrderStatus.Confirmed;

    public string StatusLabel => Status.DisplayName();
    public string StatusBadge => Status.BadgeClass();
}
