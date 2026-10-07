using System.ComponentModel.DataAnnotations;
using NEXUS.Common.Constants;
using NEXUS.Domain.Billing;
using NEXUS.Domain.Catalog;
using NEXUS.Domain.Orders;

namespace NEXUS.Domain.Organisation;

public class RetailShop : AuditableEntity
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string OutletCode { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Address { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    public int CityId { get; set; }
    public City City { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    public ICollection<Employee> Staff { get; set; } = new List<Employee>();
    public ICollection<StockItem> Stock { get; set; } = new List<StockItem>();
    public ICollection<Payment> PaymentsReceived { get; set; } = new List<Payment>();
}

public class Employee : AuditableEntity
{
    public int Id { get; set; }

    [Required, MaxLength(30)]
    public string EmployeeCode { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(30)]
    public string Designation { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? Email { get; set; }

    public DateTime JoinedOn { get; set; } = DateTime.UtcNow.Date;

    public bool IsActive { get; set; } = true;

    public int? ShopId { get; set; }
    public RetailShop? Shop { get; set; }

    public int UserId { get; set; }
    public AppUser User { get; set; } = null!;

    /** Technicians see only the jobs assigned to them. */
    public bool IsTechnician =>
        string.Equals(User?.Role, NexusRoles.Technical, StringComparison.OrdinalIgnoreCase);

    public ICollection<ConnectionOrder> AssignedOrders { get; set; } = new List<ConnectionOrder>();
    public ICollection<FeasibilityCheck> FeasibilityChecks { get; set; } = new List<FeasibilityCheck>();
}
