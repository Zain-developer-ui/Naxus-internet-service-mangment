using System.ComponentModel.DataAnnotations;

namespace NEXUS.Models.ViewModels;

/**
 * Back-office registers: the equipment in the warehouse, the vendors that
 * supply it, and the people and outlets that make up the organisation. Each
 * is a plain list + edit pair in the same shape as the plan catalogue.
 */

// ------------------------------------------------------------------- stock

public sealed class StockListPage
{
    public IReadOnlyList<StockRow> Items { get; set; } = Array.Empty<StockRow>();

    public StockTotals Totals { get; set; } = new(0, 0, 0);
    public IReadOnlyList<ShopOption> Shops { get; set; } = Array.Empty<ShopOption>();

    public int? ShopId { get; set; }
    public string? Search { get; set; }
    public bool LowOnly { get; set; }

    public bool IsEmpty => Items.Count == 0;
}

public sealed record StockTotals(int DistinctProducts, int UnitsOnHand, int BelowThreshold);

public sealed record ShopOption(int Id, string Name, string OutletCode);

public sealed record StockRow(
    int Id,
    string Sku,
    string ProductName,
    string ShopName,
    string OutletCode,
    int Quantity,
    int LowStockThreshold,
    bool IsBelowThreshold,
    bool IsActive);

public sealed class StockAdjustViewModel
{
    public int StockItemId { get; set; }

    [Required]
    [Range(-100000, 100000, ErrorMessage = "Adjustment must be between -100,000 and 100,000.")]
    [Display(Name = "Quantity Change")]
    public int QuantityDelta { get; set; }

    [Required, MaxLength(200)]
    [Display(Name = "Reason")]
    public string Reason { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;
    public string ShopName { get; set; } = string.Empty;
    public int QuantityOnHand { get; set; }
    public int LowStockThreshold { get; set; }
}

public sealed class StockMovementRow
{
    public int Id { get; set; }
    public int QuantityDelta { get; set; }
    public int QuantityAfter { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ShopName { get; set; } = string.Empty;
    public string OccurredAt { get; set; } = string.Empty;
    public string? PoNumber { get; set; }

    public bool IsIncrease => QuantityDelta >= 0;
    public string DeltaLabel => QuantityDelta >= 0 ? $"+{QuantityDelta}" : QuantityDelta.ToString();
}

public sealed class StockDetailPage
{
    public StockRow Item { get; set; } = null!;
    public IReadOnlyList<StockMovementRow> Movements { get; set; } = Array.Empty<StockMovementRow>();
    public StockAdjustViewModel Adjust { get; set; } = new();
}

// ---------------------------------------------------------------- products

public sealed class ProductListPage
{
    public IReadOnlyList<ProductRow> Products { get; set; } = Array.Empty<ProductRow>();

    public string? Search { get; set; }
    public bool ShowInactive { get; set; }

    public bool IsEmpty => Products.Count == 0;
}

public sealed record ProductRow(
    int Id,
    string Sku,
    string Name,
    string Description,
    decimal? UnitPrice,
    int LowStockThreshold,
    bool IsActive,
    int UnitsOnHand,
    int Locations);

public sealed class ProductEditViewModel
{
    public int Id { get; set; }

    [Required, MaxLength(30)]
    [Display(Name = "SKU")]
    public string Sku { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    [Display(Name = "Product Name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    /**
     * The SRS never sells equipment - it charges for replacement only. Optional
     * on purpose so the catalogue can hold stock without inventing a price.
     */
    [Range(0, 1000000, ErrorMessage = "Unit price cannot be negative.")]
    [Display(Name = "Unit Price (USD)")]
    public decimal? UnitPrice { get; set; }

    [Range(0, 100000, ErrorMessage = "Threshold cannot be negative.")]
    [Display(Name = "Low Stock Threshold")]
    public int LowStockThreshold { get; set; } = 5;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public bool IsNew => Id == 0;
    public string PageTitle => IsNew ? "New Equipment" : $"Edit {Name}";
}

// ----------------------------------------------------------------- vendors

public sealed class VendorListPage
{
    public IReadOnlyList<VendorRow> Vendors { get; set; } = Array.Empty<VendorRow>();

    public VendorTotals Totals { get; set; } = new(0, 0, 0m);

    public string? Search { get; set; }
    public bool ShowInactive { get; set; }

    public bool IsEmpty => Vendors.Count == 0;
}

public sealed record VendorTotals(int Total, int Active, decimal CommittedValue);

public sealed record VendorRow(
    int Id,
    string Name,
    string ContactPerson,
    string Phone,
    string Email,
    string Address,
    string TaxNumber,
    bool IsActive,
    int OpenOrders,
    int TotalOrders);

public sealed class VendorEditViewModel
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    [Display(Name = "Vendor Name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(120)]
    [Display(Name = "Contact Person")]
    public string ContactPerson { get; set; } = string.Empty;

    [MaxLength(20)]
    [Display(Name = "Phone")]
    public string Phone { get; set; } = string.Empty;

    [EmailAddress, MaxLength(120)]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [MaxLength(250)]
    [Display(Name = "Address")]
    public string Address { get; set; } = string.Empty;

    [MaxLength(40)]
    [Display(Name = "Tax / NTN Number")]
    public string TaxNumber { get; set; } = string.Empty;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public int PurchaseOrderCount { get; set; }

    public bool IsNew => Id == 0;
    public string PageTitle => IsNew ? "New Vendor" : $"Edit {Name}";
}

// ------------------------------------------------------- shops + employees

public sealed class OrgListPage
{
    public IReadOnlyList<ShopRow> Shops { get; set; } = Array.Empty<ShopRow>();
    public IReadOnlyList<EmployeeRow> Employees { get; set; } = Array.Empty<EmployeeRow>();
    public IReadOnlyList<ShopOption> ShopOptions { get; set; } = Array.Empty<ShopOption>();

    public OrgTotals Totals { get; set; } = new(0, 0, 0, 0);
}

public sealed record OrgTotals(int Shops, int ActiveShops, int Employees, int ActiveEmployees);

public sealed record ShopRow(
    int Id,
    string Name,
    string OutletCode,
    string CityName,
    string Address,
    string Phone,
    bool IsActive,
    int StaffCount,
    int StockLines);

public sealed class ShopEditViewModel
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    [Display(Name = "Outlet Name")]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    [Display(Name = "Outlet Code")]
    public string OutletCode { get; set; } = string.Empty;

    [MaxLength(200)]
    [Display(Name = "Address")]
    public string Address { get; set; } = string.Empty;

    [MaxLength(20)]
    [Display(Name = "Phone")]
    public string Phone { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Pick a city.")]
    [Display(Name = "City")]
    public int CityId { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public IReadOnlyList<CityOption> Cities { get; set; } = Array.Empty<CityOption>();
    public int StaffCount { get; set; }

    public bool IsNew => Id == 0;
    public string PageTitle => IsNew ? "New Outlet" : $"Edit {Name}";
}

public sealed record EmployeeRow(
    int Id,
    string EmployeeCode,
    string FullName,
    string Designation,
    string Phone,
    string Email,
    string? ShopName,
    string Role,
    string JoinedOn,
    bool IsActive,
    int? ShopId,
    int UserId,
    DateTime JoinedOnDate);

public sealed class OrgLists
{
    public IReadOnlyList<ShopOption> Shops { get; set; } = Array.Empty<ShopOption>();
    public IReadOnlyList<AppUserOption> Users { get; set; } = Array.Empty<AppUserOption>();
}

public sealed record AppUserOption(int Id, string AccountId, string DisplayName, string Role, bool AlreadyLinked);

public sealed class EmployeeEditViewModel
{
    public int Id { get; set; }

    [Required, MaxLength(30)]
    [Display(Name = "Employee Code")]
    public string EmployeeCode { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(30)]
    [Display(Name = "Designation")]
    public string Designation { get; set; } = string.Empty;

    [MaxLength(20)]
    [Display(Name = "Phone")]
    public string Phone { get; set; } = string.Empty;

    [EmailAddress, MaxLength(120)]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Display(Name = "Joined On")]
    [DataType(DataType.Date)]
    public DateTime JoinedOn { get; set; } = DateTime.UtcNow.Date;

    [Display(Name = "Assigned Outlet")]
    public int? ShopId { get; set; }

    /**
     * The login this record hangs off. Technical job filtering reads the role
     * from this user, so every employee must point at a real staff account.
     */
    [Range(1, int.MaxValue, ErrorMessage = "Pick a staff login.")]
    [Display(Name = "Staff Login")]
    public int UserId { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public IReadOnlyList<ShopOption> Shops { get; set; } = Array.Empty<ShopOption>();
    public IReadOnlyList<AppUserOption> Users { get; set; } = Array.Empty<AppUserOption>();

    public bool IsNew => Id == 0;
    public string PageTitle => IsNew ? "New Employee" : $"Edit {FullName}";
}
