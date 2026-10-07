using System.ComponentModel.DataAnnotations;
using NEXUS.Common.Constants;
using NEXUS.Domain.Billing;
using NEXUS.Domain.Orders;
using NEXUS.Domain.Organisation;
using NEXUS.Domain.Procurement;

namespace NEXUS.Domain.Customers;

public class Customer : AuditableEntity, ISoftDeletable
{
    public int Id { get; set; }

    /** SRS format: type letter + 3 digit city code + 12 digit serial. */
    [Required, MaxLength(16)]
    public string AccountId { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Cnic { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? Email { get; set; }

    [MaxLength(250)]
    public string Address { get; set; } = string.Empty;

    public int CityId { get; set; }
    public City City { get; set; } = null!;

    public ConnectionType PrimaryConnectionType { get; set; } = ConnectionType.Broadband;

    /** SRS: a Dial-Up subscriber must hold a landline with the same vendor. */
    [MaxLength(20)]
    public string? LandlineNumber { get; set; }

    public bool IsCorporate { get; set; }

    [MaxLength(120)]
    public string? OrganisationName { get; set; }

    public int? ShopId { get; set; }
    public RetailShop? RegisteredAtShop { get; set; }

    public int UserId { get; set; }
    public AppUser User { get; set; } = null!;

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public decimal SecurityDepositPaid { get; set; }

    public ICollection<Connection> Connections { get; set; } = new List<Connection>();
    public ICollection<ConnectionOrder> Orders { get; set; } = new List<ConnectionOrder>();
    public ICollection<Bill> Bills { get; set; } = new List<Bill>();
    public ICollection<Feedback> FeedbackEntries { get; set; } = new List<Feedback>();

    /** Number of live connections - drives the bulk discount tier. */
    public int LiveConnectionCount => Connections?.Count(c => !c.IsDeleted &&
        c.Status != ConnectionStatus.PermanentlyInactive) ?? 0;
}
