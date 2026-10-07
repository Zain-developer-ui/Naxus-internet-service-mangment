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

        var vm = new AccountViewModel
        {
            AccountId = customer.AccountId,
            CustomerName = customer.FullName,
            Email = customer.Email ?? string.Empty,
            Phone = customer.Phone,
            Cnic = customer.Cnic,
            Address = customer.Address,
            ConnectionType = customer.PrimaryConnectionType.DisplayName(),
            ServiceArea = customer.City?.Name ?? string.Empty
        };

        if (connection is not null)
        {
            vm.Plan = connection.Plan?.Name ?? string.Empty;
            vm.Speed = connection.Plan?.SpeedKbps is int kbps ? $"{kbps} Kbps" : string.Empty;
            vm.Status = connection.Status.DisplayName();
            vm.InstallationStatus = connection.Status.DisplayName();
            vm.ActivationDate = connection.ActivatedOn;
            vm.ServiceArea = connection.City?.Name ?? vm.ServiceArea;
        }
        else
        {
            vm.Status = order?.Status.DisplayName() ?? "No connection";
            vm.InstallationStatus = order?.Status.DisplayName() ?? string.Empty;
            vm.Plan = order?.Plan?.Name ?? string.Empty;
        }

        return Result<AccountViewModel>.Ok(vm);
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
