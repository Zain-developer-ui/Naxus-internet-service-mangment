using NEXUS.Common;
using NEXUS.Common.Constants;
using NEXUS.Data;
using NEXUS.Domain.Orders;
using NEXUS.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace NEXUS.Services.Orders;

public interface IPlanChangeService
{
    Task<Result<PlanChangePage>> GetForCustomerAsync(int customerId,
                                                     CancellationToken ct = default);

    Task<Result<int>> SubmitAsync(PlanChangeInput input, int customerId,
                                  CancellationToken ct = default);

    Task<Result<IReadOnlyList<PlanChangeRow>>> PendingAsync(CancellationToken ct = default);

    Task<Result<int>> DecideAsync(int requestId, bool approve, int? employeeId,
                                  string? remarks, CancellationToken ct = default);
}

public sealed class PlanChangeService : IPlanChangeService
{
    private readonly NexusDbContext _db;

    public PlanChangeService(NexusDbContext db) => _db = db;

    public async Task<Result<PlanChangePage>> GetForCustomerAsync(int customerId,
                                                                  CancellationToken ct = default)
    {
        var connection = await _db.Connections
            .AsNoTracking()
            .Include(c => c.Plan)
                .ThenInclude(p => p.Prices)
            .Where(c => c.CustomerId == customerId && !c.IsDeleted)
            .OrderByDescending(c => c.ActivatedOn)
            .FirstOrDefaultAsync(ct);

        if (connection is null)
            return Result<PlanChangePage>.NotFound("Connection not found.");

        var currentRate = connection.Plan.Prices
            .Where(pr => pr.Cycle == BillingCycle.Monthly && pr.IsAvailable)
            .Select(pr => (decimal?)pr.Amount).FirstOrDefault() ?? 0m;

        // A customer moves between plans of the same connection type - moving
        // a broadband customer onto a dial-up bundle would also demand a
        // landline the address may not have.
        var plans = await _db.Plans
            .AsNoTracking()
            .Where(p => p.IsActive &&
                        p.ConnectionType == connection.ConnectionType &&
                        p.Id != connection.PlanId)
            .OrderBy(p => p.SortOrder)
            .Select(p => new PlanOptionRow(
                p.Id,
                p.Name,
                p.Prices.Where(pr => pr.Cycle == BillingCycle.Monthly && pr.IsAvailable)
                        .Select(pr => (decimal?)pr.Amount).FirstOrDefault() ?? 0m,
                p.SecurityDeposit))
            .ToListAsync(ct);

        var pending = await _db.PlanChangeRequests
            .AsNoTracking()
            .AnyAsync(r => r.ConnectionId == connection.Id &&
                           r.Status == PlanChangeStatus.Pending, ct);

        return Result<PlanChangePage>.Ok(new PlanChangePage
        {
            ConnectionId = connection.Id,
            ConnectionNumber = connection.ConnectionNumber,
            ConnectionType = connection.ConnectionType.DisplayName(),
            CurrentPlanName = connection.Plan.Name,
            CurrentMonthlyRate = currentRate,
            Plans = plans,
            HasPendingRequest = pending
        });
    }

    public async Task<Result<int>> SubmitAsync(PlanChangeInput input, int customerId,
                                               CancellationToken ct = default)
    {
        var connection = await _db.Connections
            .FirstOrDefaultAsync(c => c.Id == input.ConnectionId && c.CustomerId == customerId, ct);

        if (connection is null)
            return Result<int>.NotFound("Connection not found.");

        var duplicate = await _db.PlanChangeRequests
            .AnyAsync(r => r.ConnectionId == connection.Id &&
                           r.Status == PlanChangeStatus.Pending, ct);

        if (duplicate)
            return Result<int>.Fail(ErrorKind.Validation, "A plan change for this connection is already pending.");

        if (input.PlanId == connection.PlanId)
            return Result<int>.Fail(ErrorKind.Validation, "That is the plan this connection is already on.");

        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == input.PlanId, ct);

        if (plan is null || !plan.IsActive)
            return Result<int>.Fail(ErrorKind.Validation, "Pick a plan from the list.");

        if (plan.ConnectionType != connection.ConnectionType)
            return Result<int>.Fail(ErrorKind.Validation, "Choose a plan of the same connection type.");

        var request = new PlanChangeRequest
        {
            ConnectionId = connection.Id,
            CustomerId = customerId,
            CurrentPlanId = connection.PlanId,
            RequestedPlanId = plan.Id,
            Status = PlanChangeStatus.Pending,
            Remarks = string.IsNullOrWhiteSpace(input.Remarks) ? null : input.Remarks.Trim()
        };

        _db.PlanChangeRequests.Add(request);
        await _db.SaveChangesAsync(ct);

        return Result<int>.Ok(request.Id);
    }

    public async Task<Result<IReadOnlyList<PlanChangeRow>>> PendingAsync(CancellationToken ct = default)
    {
        var rows = await _db.PlanChangeRequests
            .AsNoTracking()
            .Where(r => r.Status == PlanChangeStatus.Pending)
            .OrderBy(r => r.CreatedAt)
            .Select(r => new PlanChangeRow(
                r.Id,
                r.Connection.ConnectionNumber,
                r.Customer.AccountId,
                r.Customer.FullName,
                r.CurrentPlan.Name,
                r.RequestedPlan.Name,
                r.RequestedPlan.Prices.Where(pr => pr.Cycle == BillingCycle.Monthly && pr.IsAvailable)
                    .Select(pr => (decimal?)pr.Amount).FirstOrDefault() ?? 0m,
                r.CreatedAt))
            .ToListAsync(ct);

        return Result<IReadOnlyList<PlanChangeRow>>.Ok(rows);
    }

    public async Task<Result<int>> DecideAsync(int requestId, bool approve, int? employeeId,
                                               string? remarks, CancellationToken ct = default)
    {
        var request = await _db.PlanChangeRequests
            .Include(r => r.Connection)
            .FirstOrDefaultAsync(r => r.Id == requestId, ct);

        if (request is null)
            return Result<int>.NotFound("Request not found.");

        if (request.Status != PlanChangeStatus.Pending)
            return Result<int>.Fail(ErrorKind.Validation, "This request has already been decided.");

        request.Status = approve ? PlanChangeStatus.Approved : PlanChangeStatus.Rejected;
        request.DecidedAt = DateTime.UtcNow;
        request.DecidedByEmployeeId = employeeId;
        request.Remarks = string.IsNullOrWhiteSpace(remarks) ? request.Remarks : remarks.Trim();

        if (approve)
        {
            // The new plan takes effect from the connection record; the next
            // billing run prices the line off it automatically.
            request.Connection!.PlanId = request.RequestedPlanId;

            _db.ConnectionStatusHistory.Add(new ConnectionStatusHistory
            {
                ConnectionId = request.ConnectionId,
                FromStatus = request.Connection.Status,
                ToStatus = request.Connection.Status,
                ChangedAt = DateTime.UtcNow,
                Reason = "Plan changed on approved customer request.",
                ChangedById = employeeId
            });
        }

        await _db.SaveChangesAsync(ct);

        return Result<int>.Ok(request.Id);
    }
}
