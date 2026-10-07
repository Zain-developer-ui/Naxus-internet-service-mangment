using System.ComponentModel.DataAnnotations;
using NEXUS.Domain.Customers;

namespace NEXUS.Domain.Organisation;

public class City : AuditableEntity
{
    public int Id { get; set; }

    [Required, MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Region { get; set; } = string.Empty;

    /** Three digit code that gets embedded in an Account ID. */
    public int Code { get; set; }

    public bool IsServiced { get; set; } = true;

    public ICollection<RetailShop> Shops { get; set; } = new List<RetailShop>();
    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
}
