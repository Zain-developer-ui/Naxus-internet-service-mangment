using NEXUS.Common.Constants;
using NEXUS.Domain.Catalog;
using NEXUS.Domain.Customers;
using NEXUS.Domain.Organisation;

namespace NEXUS.Domain.Orders;

/**
 * A customer asking to move to a different plan. The SRS notes customers ask
 * for this regularly, so it is a tracked request rather than a magic switch:
 * the clerk approves it and the connection's plan changes with the next
 * billing period.
 */
public class PlanChangeRequest : AuditableEntity
{
    public int Id { get; set; }

    public int ConnectionId { get; set; }
    public Connection Connection { get; set; } = null!;

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int CurrentPlanId { get; set; }
    public Plan CurrentPlan { get; set; } = null!;

    public int RequestedPlanId { get; set; }
    public Plan RequestedPlan { get; set; } = null!;

    public PlanChangeStatus Status { get; set; } = PlanChangeStatus.Pending;

    public int? DecidedByEmployeeId { get; set; }
    public Employee? DecidedBy { get; set; }

    public DateTime? DecidedAt { get; set; }

    public string? Remarks { get; set; }
}
