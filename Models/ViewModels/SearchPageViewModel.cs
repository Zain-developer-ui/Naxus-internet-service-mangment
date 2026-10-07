namespace NEXUS.Models.ViewModels;

/**
 * One search box feeding four result sets. Each set is capped so a broad term
 * cannot drag the whole customer table into one page.
 */
public sealed class SearchPageData
{
    public string? Term { get; set; }

    public IReadOnlyList<SearchCustomerRow> Customers { get; set; } = Array.Empty<SearchCustomerRow>();
    public IReadOnlyList<SearchEmployeeRow> Employees { get; set; } = Array.Empty<SearchEmployeeRow>();
    public IReadOnlyList<SearchOrderRow> Orders { get; set; } = Array.Empty<SearchOrderRow>();
    public IReadOnlyList<SearchAccountRow> Accounts { get; set; } = Array.Empty<SearchAccountRow>();

    public bool HasTerm => !string.IsNullOrWhiteSpace(Term);
    public bool HasAnyCustomer => Customers.Count > 0;
    public bool HasAnyEmployee => Employees.Count > 0;
    public bool HasAnyOrder => Orders.Count > 0;
    public bool HasAnyAccount => Accounts.Count > 0;
}

public sealed record SearchCustomerRow(
    int CustomerId,
    string AccountId,
    string FullName,
    string Phone,
    string ServiceTypeName,
    string PlanName,
    string CityName,
    string StatusLabel,
    string StatusToken);

public sealed record SearchEmployeeRow(
    string EmployeeCode,
    string FullName,
    string RoleLabel,
    string Designation,
    string BranchName,
    string StatusLabel,
    string StatusToken);

public sealed record SearchOrderRow(
    string OrderId,
    string CustomerName,
    string ServiceTypeName,
    string PlanName,
    string StatusLabel,
    string StatusToken,
    DateTime CreatedAt);

public sealed record SearchAccountRow(
    string AccountId,
    string CustomerName,
    string ServiceTypeName,
    string PlanName,
    string BillingLabel,
    string BillingToken,
    string StatusLabel,
    string StatusToken);
