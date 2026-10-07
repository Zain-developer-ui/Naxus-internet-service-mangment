using System.ComponentModel.DataAnnotations;
using NEXUS.Common.Constants;
using NEXUS.Domain.Customers;

namespace NEXUS.Domain.Organisation;

public class AppUser : AuditableEntity, ISoftDeletable
{
    public int Id { get; set; }

    [Required, MaxLength(16)]
    public string AccountId { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? Email { get; set; }

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [Required, MaxLength(30)]
    public string Role { get; set; } = NexusRoles.Customer;

    public bool IsActive { get; set; } = true;

    public bool MustChangePassword { get; set; }

    public int AccessFailedCount { get; set; }

    public DateTime? LockoutEnd { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public bool IsLockedOut => LockoutEnd.HasValue && LockoutEnd.Value > DateTime.UtcNow;

    public Employee? Employee { get; set; }
    public Customer? Customer { get; set; }
}
