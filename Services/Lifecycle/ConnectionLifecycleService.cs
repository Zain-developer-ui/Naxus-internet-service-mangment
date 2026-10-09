using Microsoft.EntityFrameworkCore;
using NEXUS.Common;
using NEXUS.Common.Constants;
using NEXUS.Common.Extensions;
using NEXUS.Data;
using NEXUS.Domain.Orders;

namespace NEXUS.Services.Lifecycle;

/** One line the postpaid policy would move, and why. */
public sealed record OverdueAction(
    int ConnectionId,
    string ConnectionNumber,
    string CustomerName,
    string AccountId,
    decimal Outstanding,
    DateTime OldestDueOn,
    int DaysOverdue,
    ConnectionStatus From,
    ConnectionStatus To,
    string Reason);

public sealed record OverduePolicyResult(
    IReadOnlyList<OverdueAction> Actions,
    int LinesScanned,
    DateTime AsOf)
{
    public int Suspensions => Actions.Count(a => a.To == ConnectionStatus.TemporarilyInactive);
    public int Closures => Actions.Count(a => a.To == ConnectionStatus.PermanentlyInactive);
    public decimal OutstandingAtRisk => Actions.Sum(a => a.Outstanding);
}

public interface IConnectionLifecycleService
{
    /** What the policy would do right now. Changes nothing. */
    Task<Result<OverduePolicyResult>> PreviewAsync(CancellationToken ct = default);

    /** Runs the policy. Every move is written to the status history. */
    Task<Result<OverduePolicyResult>> ApplyAsync(int? employeeId, CancellationToken ct = default);
}

/**
 * The SRS ties a postpaid line's status to its bill. This is that tie: a bill
 * past its due date eventually suspends the line, and a suspension nobody pays
 * off eventually closes it.
 *
 * The sweep is deliberately not automatic on a timer. A connection going dead
 * is the kind of change an operator should be able to look at before it lands,
 * so the same query backs both the preview screen and the apply button.
 */
public sealed class ConnectionLifecycleService : IConnectionLifecycleService
{
    private readonly NexusDbContext _db;

    public ConnectionLifecycleService(NexusDbContext db) => _db = db;

    public async Task<Result<OverduePolicyResult>> PreviewAsync(CancellationToken ct = default)
        => Result<OverduePolicyResult>.Ok(await BuildAsync(DateTime.UtcNow.Date, ct));

    public async Task<Result<OverduePolicyResult>> ApplyAsync(int? employeeId, CancellationToken ct = default)
    {
        var plan = await BuildAsync(DateTime.UtcNow.Date, ct);

        if (plan.Actions.Count == 0)
            return Result<OverduePolicyResult>.Ok(plan);

        var now = DateTime.UtcNow;

        // EnableRetryOnFailure and a hand-rolled transaction cannot both own the
        // write, so the whole batch runs inside the provider's execution strategy.
        await _db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var ids = plan.Actions.Select(a => a.ConnectionId).ToList();
            var lines = await _db.Connections
                .Where(c => ids.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, ct);

            foreach (var action in plan.Actions)
            {
                // A connection may have moved since the preview was built.
                if (!lines.TryGetValue(action.ConnectionId, out var line)) continue;
                if (line.Status != action.From) continue;
                if (!line.Status.IsLegalMove(action.To)) continue;

                line.Status = action.To;
                line.DeactivatedOn = now;

                _db.ConnectionStatusHistory.Add(new ConnectionStatusHistory
                {
                    ConnectionId = line.Id,
                    FromStatus = action.From,
                    ToStatus = action.To,
                    Reason = action.Reason,
                    ChangedById = employeeId,
                    ChangedAt = now
                });
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        });

        return Result<OverduePolicyResult>.Ok(plan);
    }

    private async Task<OverduePolicyResult> BuildAsync(DateTime today, CancellationToken ct)
    {
        var lines = await _db.Connections
            .AsNoTracking()
            .Include(c => c.Customer)
            .Where(c => !c.IsDeleted && c.Status != ConnectionStatus.Pending)
            .ToListAsync(ct);

        if (lines.Count == 0)
            return new OverduePolicyResult(Array.Empty<OverdueAction>(), 0, today);

        var ids = lines.Select(c => c.Id).ToList();

        // A draft bill has not been sent yet, and a cancelled one is not owed.
        var debts = await _db.Bills
            .AsNoTracking()
            .Where(b => b.ConnectionId != null &&
                        ids.Contains(b.ConnectionId.Value) &&
                        b.Status != BillStatus.Draft &&
                        b.Status != BillStatus.Cancelled &&
                        b.Status != BillStatus.Paid &&
                        b.TotalAmount > b.AmountPaid)
            .Select(b => new
            {
                ConnectionId = b.ConnectionId!.Value,
                b.DueOn,
                Outstanding = b.TotalAmount - b.AmountPaid
            })
            .ToListAsync(ct);

        var byLine = debts
            .GroupBy(d => d.ConnectionId)
            .ToDictionary(
                g => g.Key,
                g => new { Outstanding = g.Sum(x => x.Outstanding), Oldest = g.Min(x => x.DueOn) });

        var actions = new List<OverdueAction>();

        foreach (var line in lines)
        {
            if (!byLine.TryGetValue(line.Id, out var debt)) continue;

            var days = (today - debt.Oldest.Date).Days;
            if (days < LifecycleConstants.SuspensionGraceDays) continue;

            var target = days >= LifecycleConstants.PermanentClosureDays
                ? ConnectionStatus.PermanentlyInactive
                : ConnectionStatus.TemporarilyInactive;

            // Already where the policy would put it, or a move the rules forbid.
            if (line.Status == target || !line.Status.IsLegalMove(target)) continue;

            var reason = target == ConnectionStatus.PermanentlyInactive
                ? LifecycleConstants.ClosureReason(days)
                : LifecycleConstants.SuspensionReason(days);

            actions.Add(new OverdueAction(
                line.Id,
                line.ConnectionNumber,
                line.Customer?.FullName ?? "Unknown",
                line.Customer?.AccountId ?? "-",
                debt.Outstanding,
                debt.Oldest,
                days,
                line.Status,
                target,
                reason));
        }

        return new OverduePolicyResult(
            actions.OrderByDescending(a => a.DaysOverdue).ToList(),
            lines.Count,
            today);
    }
}
