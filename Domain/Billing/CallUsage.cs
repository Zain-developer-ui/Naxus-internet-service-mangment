using System.ComponentModel.DataAnnotations;

namespace NEXUS.Domain.Billing;

/**
 * Metered landline usage for one connection over one billing period. The SRS
 * has no per-call detail - the exchange hands over a summary of minutes - so
 * this is a period total, keyed so the same window can never be entered twice.
 */
public class CallUsage : AuditableEntity
{
    public int Id { get; set; }

    public int ConnectionId { get; set; }
    public Orders.Connection Connection { get; set; } = null!;

    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    public decimal LocalMinutes { get; set; }

    public decimal StdMinutes { get; set; }

    public decimal MobileMinutes { get; set; }

    [MaxLength(300)]
    public string? Notes { get; set; }

    public bool IsBilled { get; set; }
}
