using Microsoft.EntityFrameworkCore;
using NEXUS.Common;
using NEXUS.Common.Abstractions;
using NEXUS.Common.Constants;
using NEXUS.Data;
using NEXUS.Domain.Orders;
using NEXUS.Models.ViewModels;
using NEXUS.Services.Generation;
using NEXUS.Services.Registration;

namespace NEXUS.Services.Feasibility;

/**
 * The survey queue and the connection lifecycle.
 *
 * SRS flow: an order arrives as AwaitingFeasibility -> a survey passes or fails
 * it -> a passed order is confirmed and installed -> the installation creates
 * the Connection itself, which starts as Pending and is activated once the
 * line is live. Every status change is written to ConnectionStatusHistory so
 * the audit trail the SRS asks for actually exists.
 */
public interface IFeasibilityService
{
    Task<Result<IReadOnlyList<OrderQueueItem>>> GetQueueAsync(OrderStatus? status, CancellationToken ct = default);
    Task<Result<OrderReview>> GetOrderAsync(int orderId, CancellationToken ct = default);
    Task<Result<OrderReview>> RecordCheckAsync(FeasibilityEntry form, int? employeeId, CancellationToken ct = default);
    Task<Result<OrderReview>> ConfirmAsync(int orderId, DateTime scheduledFor, int? employeeId, CancellationToken ct = default);
    Task<Result<OrderReview>> StartInstallationAsync(int orderId, int? employeeId, CancellationToken ct = default);
    Task<Result<OrderReview>> CompleteInstallationAsync(int orderId, int? employeeId, CancellationToken ct = default);
    Task<Result<OrderReview>> CancelOrderAsync(int orderId, string reason, int? employeeId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<ConnectionRow>>> GetConnectionsAsync(ConnectionStatus? status, string? search, CancellationToken ct = default);
    Task<Result<ConnectionRegisterTotals>> GetConnectionTotalsAsync(CancellationToken ct = default);
    Task<Result<ConnectionDetail>> GetConnectionAsync(int connectionId, CancellationToken ct = default);
    Task<Result<ConnectionDetail>> ChangeStatusAsync(int connectionId, ConnectionStatus target, string reason, int? employeeId, CancellationToken ct = default);
}

public sealed class FeasibilityService : IFeasibilityService
{
    private readonly NexusDbContext _db;
    private readonly IAccountIdGenerator _ids;

    public FeasibilityService(NexusDbContext db, IAccountIdGenerator ids)
    {
        _db = db;
        _ids = ids;
    }

    // ---------------------------------------------------------------- survey queue

    public async Task<Result<IReadOnlyList<OrderQueueItem>>> GetQueueAsync(OrderStatus? status, CancellationToken ct = default)
    {
        var query = _db.ConnectionOrders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Plan)
            .Include(o => o.City)
            .Include(o => o.Feasibility)
            .AsQueryable();

        // Default view is the live queue - the work still waiting on someone.
        query = status is null
            ? query.Where(o => o.Status == OrderStatus.AwaitingFeasibility ||
                               o.Status == OrderStatus.FeasibilityPassed ||
                               o.Status == OrderStatus.Confirmed ||
                               o.Status == OrderStatus.Installing)
            : query.Where(o => o.Status == status);

        var orders = await query
            .OrderBy(o => o.Status)
            .ThenBy(o => o.CreatedAt)
            .ToListAsync(ct);

        var rows = orders.Select(o => new OrderQueueItem(
            o.Id,
            o.OrderId,
            o.Customer?.FullName ?? "Unknown",
            o.Customer?.AccountId ?? "—",
            o.Plan?.Name ?? "—",
            o.ConnectionType.DisplayName(),
            o.Status,
            o.Status.DisplayName(),
            o.Feasibility?.Result ?? FeasibilityResult.Pending,
            o.Feasibility?.ResultLabel ?? "Pending",
            o.Feasibility?.DistanceFromExchangeKm,
            o.City?.Name ?? "—",
            o.QuotedAmount,
            o.CreatedAt,
            o.ScheduledFor)).ToList();

        return Result<IReadOnlyList<OrderQueueItem>>.Ok(rows);
    }

    public async Task<Result<OrderReview>> GetOrderAsync(int orderId, CancellationToken ct = default)
    {
        var order = await LoadOrderAsync(orderId, ct);
        return order is null
            ? Result<OrderReview>.NotFound($"No order exists with id {orderId}.")
            : Result<OrderReview>.Ok(ToReview(order));
    }

    // ---------------------------------------------------------------- feasibility check

    public async Task<Result<OrderReview>> RecordCheckAsync(FeasibilityEntry form, int? employeeId, CancellationToken ct = default)
    {
        var order = await LoadOrderAsync(form.OrderId, ct);
        if (order is null)
            return Result<OrderReview>.NotFound($"No order exists with id {form.OrderId}.");

        if (order.Status != OrderStatus.AwaitingFeasibility)
            return Result<OrderReview>.Conflict(
                $"{order.OrderId} is {order.Status.DisplayName()} - the survey has already been recorded.");

        /**
         * The outcome is decided by the rules, not by the officer. They supply
         * the three facts; pass/fail and the reason follow from them. That way
         * a run of orders cannot be waved through by hand.
         */
        var outcome = Evaluate(order, form);
        var now = DateTime.UtcNow;

        order.Feasibility ??= new FeasibilityCheck { OrderId = order.Id };
        order.Feasibility.Result = outcome.Result;
        order.Feasibility.DistanceFromExchangeKm = form.DistanceFromExchangeKm;
        order.Feasibility.ExchangeHasCapacity = form.ExchangeHasCapacity;
        order.Feasibility.LandlineVerified = form.LandlineVerified;
        order.Feasibility.Remarks = outcome.Remarks;
        order.Feasibility.CheckedById = employeeId;
        order.Feasibility.CheckedAt = now;

        if (outcome.Result == FeasibilityResult.Passed)
        {
            order.Status = OrderStatus.FeasibilityPassed;
        }
        else
        {
            order.Status = OrderStatus.FeasibilityFailed;
        }

        if (order.Feasibility.Id == 0) _db.FeasibilityChecks.Add(order.Feasibility);

        await _db.SaveChangesAsync(ct);
        return Result<OrderReview>.Ok(ToReview(order));
    }

    private static (FeasibilityResult Result, string Remarks) Evaluate(ConnectionOrder order, FeasibilityEntry form)
    {
        // Landline first - it is a hard requirement the officer cannot waive by
        // entering a short distance.
        if (FeasibilityRules.NeedsLandline(order.ConnectionType))
        {
            if (string.IsNullOrWhiteSpace(order.LandlineNumber))
                return (FeasibilityResult.Failed, FeasibilityRules.LandlineNotDocumented);

            if (!form.LandlineVerified)
                return (FeasibilityResult.Failed, FeasibilityRules.LandlineMissing);
        }

        if (FeasibilityRules.ExceedsHardLimit(form.DistanceFromExchangeKm))
            return (FeasibilityResult.Failed,
                $"{FeasibilityRules.DistanceTooFar} ({form.DistanceFromExchangeKm:0.#} km)");

        if (!form.ExchangeHasCapacity)
            return (FeasibilityResult.Failed, FeasibilityRules.NoCapacity);

        var note = form.Remarks.Trim();
        if (FeasibilityRules.ExceedsSoftLimit(form.DistanceFromExchangeKm))
        {
            var warning = $"Passed at {form.DistanceFromExchangeKm:0.#} km - a signal booster may be required.";
            note = string.IsNullOrEmpty(note) ? warning : warning + " " + note;
        }

        return (FeasibilityResult.Passed, note);
    }

    // ---------------------------------------------------------------- order lifecycle

    public async Task<Result<OrderReview>> ConfirmAsync(int orderId, DateTime scheduledFor, int? employeeId, CancellationToken ct = default)
    {
        var order = await LoadOrderAsync(orderId, ct);
        if (order is null)
            return Result<OrderReview>.NotFound($"No order exists with id {orderId}.");

        if (order.Status != OrderStatus.FeasibilityPassed)
            return Result<OrderReview>.Conflict(
                $"{order.OrderId} is {order.Status.DisplayName()} - only a passed survey can be confirmed.");

        if (scheduledFor.Date < DateTime.UtcNow.Date)
            return Result<OrderReview>.Fail(nameof(scheduledFor),
                "The installation date cannot be in the past.");

        order.Status = OrderStatus.Confirmed;
        order.ScheduledFor = scheduledFor.Date;
        await _db.SaveChangesAsync(ct);
        return Result<OrderReview>.Ok(ToReview(order));
    }

    public async Task<Result<OrderReview>> StartInstallationAsync(int orderId, int? employeeId, CancellationToken ct = default)
    {
        var order = await LoadOrderAsync(orderId, ct);
        if (order is null)
            return Result<OrderReview>.NotFound($"No order exists with id {orderId}.");

        if (order.Status != OrderStatus.Confirmed)
            return Result<OrderReview>.Conflict(
                $"{order.OrderId} is {order.Status.DisplayName()} - confirm the order before starting work.");

        order.Status = OrderStatus.Installing;
        await _db.SaveChangesAsync(ct);
        return Result<OrderReview>.Ok(ToReview(order));
    }

    public async Task<Result<OrderReview>> CompleteInstallationAsync(int orderId, int? employeeId, CancellationToken ct = default)
    {
        var order = await LoadOrderAsync(orderId, ct);
        if (order is null)
            return Result<OrderReview>.NotFound($"No order exists with id {orderId}.");

        if (order.Status != OrderStatus.Installing)
            return Result<OrderReview>.Conflict(
                $"{order.OrderId} is {order.Status.DisplayName()} - start the installation first.");

        // One order, one line: the unique index on Connection.OrderId enforces
        // this in the database as well.
        if (order.Connection is not null)
            return Result<OrderReview>.Conflict($"{order.OrderId} already has a connection.");

        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var number = await _ids.NextConnectionNumberAsync(order.ConnectionType, ct);

            var connection = new Connection
            {
                ConnectionNumber = number,
                CustomerId = order.CustomerId,
                PlanId = order.PlanId,
                OrderId = order.Id,
                ConnectionType = order.ConnectionType,
                // The line is live at the exchange but not yet the customer's
                // to use, so it starts Pending until activation.
                Status = ConnectionStatus.Pending,
                ActivatedOn = DateTime.UtcNow,
                LandlineNumber = order.LandlineNumber,
                ServiceAddress = order.InstallationAddress,
                CityId = order.CityId
            };

            _db.Connections.Add(connection);
            await _db.SaveChangesAsync(ct);

            _db.ConnectionStatusHistory.Add(new ConnectionStatusHistory
            {
                ConnectionId = connection.Id,
                FromStatus = ConnectionStatus.Pending,
                ToStatus = ConnectionStatus.Pending,
                Reason = $"Line created from order {order.OrderId}.",
                ChangedAt = DateTime.UtcNow,
                ChangedById = employeeId
            });

            order.Status = OrderStatus.Completed;
            order.CompletedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        });

        var saved = await LoadOrderAsync(orderId, ct);
        return Result<OrderReview>.Ok(ToReview(saved!));
    }

    public async Task<Result<OrderReview>> CancelOrderAsync(int orderId, string reason, int? employeeId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Result<OrderReview>.Fail(nameof(reason), "A cancellation reason is required.");

        var order = await LoadOrderAsync(orderId, ct);
        if (order is null)
            return Result<OrderReview>.NotFound($"No order exists with id {orderId}.");

        if (order.Status == OrderStatus.Completed)
            return Result<OrderReview>.Conflict(
                $"{order.OrderId} is already completed - cancel the connection instead.");

        if (order.Status == OrderStatus.Cancelled)
            return Result<OrderReview>.Conflict($"{order.OrderId} is already cancelled.");

        order.Status = OrderStatus.Cancelled;
        order.Notes = string.IsNullOrWhiteSpace(order.Notes)
            ? $"Cancelled: {reason.Trim()}"
            : $"{order.Notes}\nCancelled: {reason.Trim()}";

        await _db.SaveChangesAsync(ct);
        return Result<OrderReview>.Ok(ToReview(order));
    }

    // ---------------------------------------------------------------- connections

    public async Task<Result<IReadOnlyList<ConnectionRow>>> GetConnectionsAsync(ConnectionStatus? status, string? search, CancellationToken ct = default)
    {
        var query = _db.Connections
            .AsNoTracking()
            .Include(c => c.Customer)
            .Include(c => c.Plan)
            .Include(c => c.City)
            .Where(c => !c.IsDeleted);

        if (status is not null) query = query.Where(c => c.Status == status);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c =>
                c.ConnectionNumber.Contains(term) ||
                c.Customer!.FullName.Contains(term) ||
                c.Customer!.AccountId.Contains(term));
        }

        var connections = await query
            .OrderByDescending(c => c.ActivatedOn)
            .ToListAsync(ct);

        var rows = connections.Select(c => new ConnectionRow(
            c.Id,
            c.ConnectionNumber,
            c.Customer?.FullName ?? "Unknown",
            c.Customer?.AccountId ?? "—",
            c.Plan?.Name ?? "—",
            c.ConnectionType.DisplayName(),
            c.Status,
            c.StatusLabel,
            c.ActivatedOn,
            c.DeactivatedOn,
            c.ServiceAddress,
            c.City?.Name ?? "—",
            c.IsBillable)).ToList();

        return Result<IReadOnlyList<ConnectionRow>>.Ok(rows);
    }

    /**
     * Register totals ignore the active filter and search term on purpose - the
     * header cards are meant to summarise the whole estate. Deriving them from
     * the filtered page would make every card read zero the moment an operator
     * filtered down to a status that happens to have no rows.
     */
    public async Task<Result<ConnectionRegisterTotals>> GetConnectionTotalsAsync(CancellationToken ct = default)
    {
        var live = _db.Connections.AsNoTracking().Where(c => !c.IsDeleted);

        var active = await live.CountAsync(c => c.Status == ConnectionStatus.Active, ct);
        var pending = await live.CountAsync(c => c.Status == ConnectionStatus.Pending, ct);
        var suspended = await live.CountAsync(c => c.Status == ConnectionStatus.TemporarilyInactive, ct);
        var closed = await live.CountAsync(c => c.Status == ConnectionStatus.PermanentlyInactive, ct);

        return Result<ConnectionRegisterTotals>.Ok(
            new ConnectionRegisterTotals(active, pending, suspended, closed, active));
    }

    public async Task<Result<ConnectionDetail>> GetConnectionAsync(int connectionId, CancellationToken ct = default)
    {
        var connection = await _db.Connections
            .AsNoTracking()
            .Include(c => c.Customer)
            .Include(c => c.Plan)
            .Include(c => c.City)
            .Include(c => c.Order)
            .Include(c => c.Bills)
            .Include(c => c.StatusHistory)
            .FirstOrDefaultAsync(c => c.Id == connectionId && !c.IsDeleted, ct);

        return connection is null
            ? Result<ConnectionDetail>.NotFound($"No connection exists with id {connectionId}.")
            : Result<ConnectionDetail>.Ok(ToDetail(connection));
    }

    /**
     * The only place a connection status changes. Legal moves are listed
     * explicitly so no path accidentally reactivates a permanently closed line.
     */
    public async Task<Result<ConnectionDetail>> ChangeStatusAsync(int connectionId, ConnectionStatus target, string reason, int? employeeId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Result<ConnectionDetail>.Fail(nameof(reason), "A reason is required for a status change.");

        var connection = await _db.Connections
            .FirstOrDefaultAsync(c => c.Id == connectionId && !c.IsDeleted, ct);

        if (connection is null)
            return Result<ConnectionDetail>.NotFound($"No connection exists with id {connectionId}.");

        var from = connection.Status;

        if (from == target)
            return Result<ConnectionDetail>.Conflict(
                $"{connection.ConnectionNumber} is already {target.DisplayName()}.");

        if (!IsLegalMove(from, target))
            return Result<ConnectionDetail>.Conflict(
                $"{connection.ConnectionNumber} cannot move from {from.DisplayName()} to {target.DisplayName()}.");

        connection.Status = target;
        connection.DeactivatedOn = target == ConnectionStatus.Active ? null : DateTime.UtcNow;

        _db.ConnectionStatusHistory.Add(new ConnectionStatusHistory
        {
            ConnectionId = connection.Id,
            FromStatus = from,
            ToStatus = target,
            Reason = reason.Trim(),
            ChangedAt = DateTime.UtcNow,
            ChangedById = employeeId
        });

        await _db.SaveChangesAsync(ct);
        return await GetConnectionAsync(connectionId, ct) is { IsSuccess: true } refreshed
            ? Result<ConnectionDetail>.Ok(refreshed.Value!)
            : Result<ConnectionDetail>.Ok(ToDetail(connection));
    }

    private static bool IsLegalMove(ConnectionStatus from, ConnectionStatus to) => (from, to) switch
    {
        // A new line goes live.
        (ConnectionStatus.Pending, ConnectionStatus.Active) => true,

        // A line can be suspended by either party, temporarily or for good.
        (ConnectionStatus.Pending, ConnectionStatus.TemporarilyInactive) => true,
        (ConnectionStatus.Pending, ConnectionStatus.PermanentlyInactive) => true,
        (ConnectionStatus.Active, ConnectionStatus.TemporarilyInactive) => true,
        (ConnectionStatus.Active, ConnectionStatus.PermanentlyInactive) => true,

        // Only a temporary suspension can be reversed.
        (ConnectionStatus.TemporarilyInactive, ConnectionStatus.Active) => true,
        (ConnectionStatus.TemporarilyInactive, ConnectionStatus.PermanentlyInactive) => true,

        _ => false
    };

    // ---------------------------------------------------------------- helpers

    private Task<ConnectionOrder?> LoadOrderAsync(int orderId, CancellationToken ct) =>
        _db.ConnectionOrders
            .Include(o => o.Customer)
            .Include(o => o.Plan)
            .Include(o => o.City)
            .Include(o => o.Feasibility)
            .Include(o => o.Connection)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

    private static OrderReview ToReview(ConnectionOrder o) => new(
        o.Id,
        o.OrderId,
        o.Customer?.FullName ?? "Unknown",
        o.Customer?.AccountId ?? "—",
        o.Customer?.Cnic ?? "—",
        o.Customer?.Phone ?? "—",
        o.Plan?.Name ?? "—",
        o.ConnectionType,
        o.ConnectionType.DisplayName(),
        o.BillingCycle.DisplayName(),
        o.Status,
        o.Status.DisplayName(),
        o.InstallationAddress,
        o.City?.Name ?? "—",
        o.LandlineNumber,
        o.Notes,
        o.QuotedAmount,
        o.PreferredSlot,
        o.ScheduledFor,
        o.CompletedAt,
        o.CreatedAt,
        o.Feasibility?.Result ?? FeasibilityResult.Pending,
        o.Feasibility?.ResultLabel ?? "Pending",
        o.Feasibility?.DistanceFromExchangeKm,
        o.Feasibility?.ExchangeHasCapacity ?? false,
        o.Feasibility?.LandlineVerified ?? false,
        o.Feasibility?.Remarks ?? string.Empty,
        o.Feasibility?.CheckedAt,
        o.Connection?.Id,
        o.Connection?.ConnectionNumber,
        o.Connection?.Status,
        o.Connection is null ? null : o.Connection.StatusLabel);

    private static ConnectionDetail ToDetail(Connection c) => new(
        c.Id,
        c.ConnectionNumber,
        c.CustomerId,
        c.Customer?.FullName ?? "Unknown",
        c.Customer?.AccountId ?? "—",
        c.Customer?.Phone ?? "—",
        c.Customer?.Email ?? "—",
        c.Plan?.Name ?? "—",
        c.ConnectionType,
        c.ConnectionType.DisplayName(),
        c.Status,
        c.StatusLabel,
        c.IsBillable,
        c.ActivatedOn,
        c.DeactivatedOn,
        c.ServiceAddress,
        c.City?.Name ?? "—",
        c.LandlineNumber,
        c.Order?.OrderId ?? "—",
        c.Bills.Count,
        c.Bills.Where(b => b.Status != BillStatus.Paid && b.Status != BillStatus.Cancelled)
            .Sum(b => b.TotalAmount - b.AmountPaid),
        c.StatusHistory
            .OrderByDescending(h => h.ChangedAt)
            .Select(h => new StatusChangeRow(
                h.FromStatus.DisplayName(),
                h.ToStatus.DisplayName(),
                h.Reason,
                h.ChangedAt))
            .ToList(),
        AllowedMoves(c.Status));

    private static IReadOnlyList<StatusMove> AllowedMoves(ConnectionStatus from)
    {
        var all = new[]
        {
            ConnectionStatus.Active,
            ConnectionStatus.TemporarilyInactive,
            ConnectionStatus.PermanentlyInactive
        };

        return all.Where(t => t != from && IsLegalMove(from, t))
                  .Select(t => new StatusMove(t, t.DisplayName()))
                  .ToList();
    }
}
