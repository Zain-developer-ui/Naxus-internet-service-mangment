using System.ComponentModel.DataAnnotations;
using NEXUS.Common.Constants;
using NEXUS.Domain.Billing;
using NEXUS.Domain.Catalog;
using NEXUS.Domain.Customers;
using NEXUS.Domain.Organisation;

namespace NEXUS.Domain.Orders;

public class Connection : AuditableEntity, ISoftDeletable, IConcurrencyGuarded
{
    public int Id { get; set; }

    [Required, MaxLength(30)]
    public string ConnectionNumber { get; set; } = string.Empty;

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int PlanId { get; set; }
    public Plan Plan { get; set; } = null!;

    public int OrderId { get; set; }
    public ConnectionOrder Order { get; set; } = null!;

    public ConnectionType ConnectionType { get; set; }

    public ConnectionStatus Status { get; set; } = ConnectionStatus.Pending;

    public DateTime ActivatedOn { get; set; }

    public DateTime? DeactivatedOn { get; set; }

    [MaxLength(20)]
    public string? LandlineNumber { get; set; }

    [MaxLength(250)]
    public string ServiceAddress { get; set; } = string.Empty;

    public int CityId { get; set; }
    public City City { get; set; } = null!;

    public byte[]? RowVersion { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<ConnectionStatusHistory> StatusHistory { get; set; } = new List<ConnectionStatusHistory>();
    public ICollection<Bill> Bills { get; set; } = new List<Bill>();

    public string StatusLabel => Status.DisplayName();
    public bool IsBillable => Status.CanBeBilled();
}

/** Append only. Every status change is recorded so the SRS audit trail holds. */
public class ConnectionStatusHistory : AuditableEntity
{
    public int Id { get; set; }

    public int ConnectionId { get; set; }
    public Connection Connection { get; set; } = null!;

    public ConnectionStatus FromStatus { get; set; }
    public ConnectionStatus ToStatus { get; set; }

    [MaxLength(300)]
    public string Reason { get; set; } = string.Empty;

    public int? ChangedById { get; set; }
    public Employee? ChangedBy { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
