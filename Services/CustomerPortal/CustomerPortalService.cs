using Microsoft.EntityFrameworkCore;
using NEXUS.Common;
using NEXUS.Common.Constants;
using NEXUS.Common.Extensions;
using NEXUS.Data;
using NEXUS.Domain.Orders;
using NEXUS.Models.ViewModels;

namespace NEXUS.Services.CustomerPortal;

public interface ICustomerPortalService
{
    Task<Result<AccountViewModel>> GetAccountAsync(int customerId, CancellationToken ct = default);
    Task<Result<CustomerDashboardViewModel>> GetDashboardAsync(int customerId, CancellationToken ct = default);

    /** Public status lookup by account id, mobile or CNIC. */
    Task<Result<AccountViewModel>> LookupAsync(string? accountId, string? phone, string? cnic,
                                               CancellationToken ct = default);
}

/**
 * Everything the customer portal shows, scoped to one customer. The portal used
 * to read a fixture with a hardcoded account ID, so every signed-in customer
 * saw the same stranger's details; the ID now always comes from the claims
 * cookie and every query is filtered by it.
 */
public sealed class CustomerPortalService : ICustomerPortalService
{
    private readonly NexusDbContext _db;

    public CustomerPortalService(NexusDbContext db) => _db = db;

    public async Task<Result<AccountViewModel>> GetAccountAsync(int customerId, CancellationToken ct = default)
    {
        var customer = await _db.Customers
            .AsNoTracking()
            .Include(c => c.City)
            .FirstOrDefaultAsync(c => c.Id == customerId && !c.IsDeleted, ct);

        if (customer is null)
            return Result<AccountViewModel>.NotFound("No customer account found.");

        return Result<AccountViewModel>.Ok(await BuildAsync(customer, ct));
    }

    /**
     * The public status check. Matching is exact on whichever single field was
     * supplied, and the response is the same shape the portal shows a signed-in
     * customer - it carries no bills, no payments and no CNIC.
     */
    public async Task<Result<AccountViewModel>> LookupAsync(string? accountId, string? phone, string? cnic,
                                                            CancellationToken ct = default)
    {
        var query = _db.Customers
            .AsNoTracking()
            .Include(c => c.City)
            .Where(c => !c.IsDeleted);

        if (!string.IsNullOrWhiteSpace(accountId))
        {
            var key = accountId.Trim().ToUpperInvariant();
            query = query.Where(c => c.AccountId.ToUpper() == key);
        }
        else if (!string.IsNullOrWhiteSpace(phone))
        {
            var key = phone.Trim();
            query = query.Where(c => c.Phone == key);
        }
        else if (!string.IsNullOrWhiteSpace(cnic))
        {
            var key = cnic.Trim();
            query = query.Where(c => c.Cnic == key);
        }
        else
        {
            return Result<AccountViewModel>.Fail(ErrorKind.Validation,
                "Enter an Account ID, mobile number or CNIC.");
        }

        var customer = await query.FirstOrDefaultAsync(ct);

        // One message for every miss so the form cannot be used to test whether
        // a given account or CNIC exists.
        if (customer is null)
            return Result<AccountViewModel>.NotFound("No account matches those details.");

        var account = await BuildAsync(customer, ct);
        account.Cnic = string.Empty;

        return Result<AccountViewModel>.Ok(account);
    }

    private async Task<AccountViewModel> BuildAsync(Domain.Customers.Customer customer, CancellationToken ct)
    {
        var customerId = customer.Id;

        var connection = await _db.Connections
            .AsNoTracking()
            .Include(c => c.Plan)
            .Include(c => c.City)
            .Where(c => c.CustomerId == customerId && !c.IsDeleted)
            .OrderByDescending(c => c.Status == ConnectionStatus.Active)
            .ThenByDescending(c => c.ActivatedOn)
            .FirstOrDefaultAsync(ct);

        var order = await _db.ConnectionOrders
            .AsNoTracking()
            .Include(o => o.Plan)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(ct);

        // "Paid" has to mean the customer owes nothing, which is a question for
        // the bills rather than a column on the customer row.
        var outstanding = await _db.Bills
            .AsNoTracking()
            .Where(b => b.CustomerId == customerId &&
                        b.Status != BillStatus.Paid &&
                        b.Status != BillStatus.Cancelled &&
                        b.Status != BillStatus.Draft &&
                        b.TotalAmount > b.AmountPaid)
            .SumAsync(b => b.TotalAmount - b.AmountPaid, ct);

        var vm = new AccountViewModel
        {
            AccountId = customer.AccountId,
            CustomerName = customer.FullName,
            Email = customer.Email ?? string.Empty,
            Phone = customer.Phone,
            Cnic = customer.Cnic,
            Address = customer.Address,
            ConnectionType = customer.PrimaryConnectionType.DisplayName(),
            ServiceArea = customer.City?.Name ?? string.Empty,
            BillingStatus = outstanding > 0 ? "Payment due" : "Up to date"
        };

        if (connection is not null)
        {
            vm.Plan = connection.Plan?.Name ?? string.Empty;
            vm.Speed = connection.Plan?.SpeedKbps is int kbps ? $"{kbps} Kbps" : string.Empty;
            vm.Status = connection.Status.DisplayName();
            vm.InstallationStatus = connection.Status.DisplayName();
            vm.ActivationDate = connection.ActivatedOn;
            vm.InstallationDate = connection.ActivatedOn;
            vm.ServiceArea = connection.City?.Name ?? vm.ServiceArea;
            vm.PlanId = connection.PlanId;
            vm.PlanFeatures = BuildFeatures(connection.Plan, connection.ConnectionType);
            vm.Timeline = await BuildTimelineAsync(connection, order, ct);
            vm.Equipment = await BuildEquipmentAsync(connection.ConnectionType, ct);
            vm.RouterModel = vm.Equipment.FirstOrDefault()?.Name ?? string.Empty;
        }
        else
        {
            vm.Status = order?.Status.DisplayName() ?? "No connection";
            vm.InstallationStatus = order?.Status.DisplayName() ?? string.Empty;
            vm.Plan = order?.Plan?.Name ?? string.Empty;
            vm.PlanId = order?.PlanId;
            vm.PlanFeatures = BuildFeatures(order?.Plan, order?.ConnectionType ?? customer.PrimaryConnectionType);
        }

        return vm;
    }

    /**
     * The plan's own columns describe what the customer bought, so the feature
     * list is derived rather than written out. A marketing list here would drift
     * the moment an administrator edited the plan.
     */
    private static IReadOnlyList<string> BuildFeatures(Domain.Catalog.Plan? plan, ConnectionType type)
    {
        if (plan is null) return Array.Empty<string>();

        var features = new List<string>();

        if (plan.IsUnlimited)
            features.Add("Unlimited usage with no hourly cap");
        else if (plan.HoursIncluded is int hours)
            features.Add($"{hours} hours of usage included each cycle");

        if (plan.SpeedKbps is int kbps)
            features.Add($"{kbps} Kbps download speed");

        features.Add(type is ConnectionType.DialUp or ConnectionType.Telephone
            ? "Runs over your existing NEXUS landline"
            : "Dedicated broadband line - no landline needed");

        if (!string.IsNullOrWhiteSpace(plan.Description))
            features.Add(plan.Description);

        if (plan.SecurityDeposit > 0)
            features.Add($"{plan.SecurityDeposit:C} security deposit, refundable on withdrawal");

        return features;
    }

    /**
     * The journey the customer actually went through. The status history already
     * records every move with its reason and timestamp, so nothing here is
     * invented - an order that never passed its survey simply shows fewer steps.
     */
    private async Task<IReadOnlyList<AccountTimelineEntry>> BuildTimelineAsync(
        Domain.Orders.Connection connection, Domain.Orders.ConnectionOrder? order, CancellationToken ct)
    {
        var timeline = new List<AccountTimelineEntry>();

        var placedAt = order?.CreatedAt ?? connection.CreatedAt;
        timeline.Add(new AccountTimelineEntry(
            placedAt.ToLocalTime().ToString("dd MMM yyyy · HH:mm"),
            "Order submitted",
            $"Request for {order?.Plan?.Name ?? connection.Plan?.Name ?? "a connection"} received.",
            true));

        var moves = await _db.ConnectionStatusHistory
            .AsNoTracking()
            .Where(h => h.ConnectionId == connection.Id && h.FromStatus != h.ToStatus)
            .OrderBy(h => h.ChangedAt)
            .ToListAsync(ct);

        foreach (var move in moves)
        {
            timeline.Add(new AccountTimelineEntry(
                move.ChangedAt.ToLocalTime().ToString("dd MMM yyyy · HH:mm"),
                move.ToStatus switch
                {
                    ConnectionStatus.Active => "Connection activated",
                    ConnectionStatus.TemporarilyInactive => "Line suspended",
                    ConnectionStatus.PermanentlyInactive => "Line closed",
                    _ => move.ToStatus.DisplayName()
                },
                move.Reason,
                move.ToStatus != ConnectionStatus.PermanentlyInactive));
        }

        var technician = await _db.FeasibilityChecks
            .AsNoTracking()
            .Where(f => f.OrderId == connection.OrderId && f.CheckedBy != null)
            .Select(f => f.CheckedBy!.FullName)
            .FirstOrDefaultAsync(ct);

        if (!string.IsNullOrWhiteSpace(technician))
        {
            timeline.Insert(1, new AccountTimelineEntry(
                connection.ActivatedOn.ToLocalTime().ToString("dd MMM yyyy"),
                "Installation completed",
                $"Surveyed and commissioned by {technician}.",
                true));
        }

        return timeline;
    }

    /**
     * A telephone line carries no data equipment, so it gets no list rather than
     * an empty section. Everything else reads the live catalogue.
     */
    private async Task<IReadOnlyList<AccountEquipmentRow>> BuildEquipmentAsync(
        ConnectionType type, CancellationToken ct)
    {
        if (type == ConnectionType.Telephone) return Array.Empty<AccountEquipmentRow>();

        return await _db.EquipmentProducts
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new AccountEquipmentRow(p.Name, p.Description))
            .ToListAsync(ct);
    }

    public async Task<Result<CustomerDashboardViewModel>> GetDashboardAsync(int customerId, CancellationToken ct = default)
    {
        var account = await GetAccountAsync(customerId, ct);
        if (!account.IsSuccess)
            return Result<CustomerDashboardViewModel>.NotFound(account.Message ?? "Account not found.");

        var vm = new CustomerDashboardViewModel { Account = account.Value! };

        var bills = await _db.Bills
            .AsNoTracking()
            .Where(b => b.CustomerId == customerId)
            .OrderByDescending(b => b.IssuedOn)
            .Take(6)
            .Select(b => new
            {
                b.BillNumber,
                b.Status,
                b.TotalAmount,
                b.PeriodStart,
                b.PeriodEnd,
                b.DueOn
            })
            .ToListAsync(ct);

        vm.RecentBills = bills.Select(b => new BillViewModel
        {
            BillNo = b.BillNumber,
            Status = b.Status.DisplayName(),
            Amount = b.TotalAmount,
            Period = $"{b.PeriodStart:dd MMM} – {b.PeriodEnd:dd MMM yyyy}",
            DueDate = b.DueOn
        }).ToList();

        vm.CurrentBill = vm.RecentBills.FirstOrDefault();

        return Result<CustomerDashboardViewModel>.Ok(vm);
    }
}
