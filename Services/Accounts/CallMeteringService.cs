using Microsoft.EntityFrameworkCore;
using NEXUS.Common;
using NEXUS.Common.Constants;
using NEXUS.Data;
using NEXUS.Domain.Billing;
using NEXUS.Domain.Orders;
using NEXUS.Models.ViewModels;

namespace NEXUS.Services.Accounts;

/**
 * Call metering for landline and dial-up lines. The SRS bills calls per
 * minute off a monthly meter reading, so this records the reading and the
 * next bill for the same window turns it into charge lines.
 */
public interface ICallMeteringService
{
    Task<Result<CallMeteringPage>> LoadAsync(CancellationToken ct = default);
    Task<Result<int>> SaveAsync(CallMeterInput input, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(int id, CancellationToken ct = default);
}

public sealed class CallMeteringService : ICallMeteringService
{
    private readonly NexusDbContext _db;

    public CallMeteringService(NexusDbContext db) => _db = db;

    public async Task<Result<CallMeteringPage>> LoadAsync(CancellationToken ct = default)
    {
        var entries = await _db.CallUsages
            .AsNoTracking()
            .Include(u => u.Connection).ThenInclude(c => c.Customer)
            .Include(u => u.Connection).ThenInclude(c => c.Plan)
            .OrderByDescending(u => u.PeriodStart)
            .ThenByDescending(u => u.Id)
            .Take(200)
            .Select(u => new CallMeterRow(
                u.Id,
                u.ConnectionId,
                u.Connection.ConnectionNumber,
                u.Connection.Customer.FullName,
                u.PeriodStart,
                u.PeriodEnd,
                u.LocalMinutes,
                u.StdMinutes,
                u.MobileMinutes,
                u.Connection.Plan.CallTariffLabel ?? "-",
                u.IsBilled))
            .ToListAsync(ct);

        /**
         * Only lines that can actually carry calls are offered. A broadband
         * plan has no tariff, so metering it would produce a reading that
         * never turns into a charge.
         */
        var lines = await _db.Connections
            .AsNoTracking()
            .Include(c => c.Customer)
            .Include(c => c.Plan)
            .Where(c => !c.IsDeleted &&
                        c.Plan.CallTariffLabel != null &&
                        (c.ConnectionType == ConnectionType.Telephone ||
                         c.ConnectionType == ConnectionType.DialUp))
            .OrderBy(c => c.ConnectionNumber)
            .Select(c => new CallLineOption(
                c.Id,
                c.ConnectionNumber,
                c.Customer.AccountId,
                c.Customer.FullName,
                c.Plan.Name,
                c.LandlineNumber,
                c.Plan.CallTariffLabel!))
            .ToListAsync(ct);

        return Result<CallMeteringPage>.Ok(new CallMeteringPage
        {
            Entries = entries,
            Lines = lines
        });
    }

    public async Task<Result<int>> SaveAsync(CallMeterInput input, CancellationToken ct = default)
    {
        if (input.PeriodEnd < input.PeriodStart)
            return Result<int>.Fail(nameof(input.PeriodEnd), "The period end cannot be before its start.");

        var connection = await _db.Connections
            .Include(c => c.Plan)
            .FirstOrDefaultAsync(c => c.Id == input.ConnectionId, ct);

        if (connection is null) return Result<int>.NotFound("Connection");

        if (connection.Plan.CallTariffLabel is null)
            return Result<int>.Fail(nameof(input.ConnectionId),
                "That plan carries no call tariff, so calls cannot be metered against it.");

        if (input.LocalMinutes <= 0m && input.StdMinutes <= 0m && input.MobileMinutes <= 0m)
            return Result<int>.Fail(nameof(input.LocalMinutes), "Enter at least one minute figure.");

        /**
         * One reading per window. Editing an existing one is how a correction
         * is made, so a repeat save updates rather than piling up.
         */
        var existing = await _db.CallUsages.FirstOrDefaultAsync(u =>
            u.ConnectionId == input.ConnectionId &&
            u.PeriodStart == input.PeriodStart.Date &&
            u.PeriodEnd == input.PeriodEnd.Date, ct);

        if (existing is not null)
        {
            if (existing.IsBilled)
                return Result<int>.Conflict("This reading has been billed and can no longer be changed.");

            existing.LocalMinutes = input.LocalMinutes;
            existing.StdMinutes = input.StdMinutes;
            existing.MobileMinutes = input.MobileMinutes;
            existing.Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim();
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            existing = new CallUsage
            {
                ConnectionId = input.ConnectionId,
                PeriodStart = input.PeriodStart.Date,
                PeriodEnd = input.PeriodEnd.Date,
                LocalMinutes = input.LocalMinutes,
                StdMinutes = input.StdMinutes,
                MobileMinutes = input.MobileMinutes,
                Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim()
            };

            _db.CallUsages.Add(existing);
        }

        await _db.SaveChangesAsync(ct);
        return Result<int>.Ok(existing.Id);
    }

    /**
     * A reading that has already been billed is part of a customer's invoice
     * history - deleting it would leave the bill's call lines unexplainable,
     * so it is refused.
     */
    public async Task<Result<bool>> DeleteAsync(int id, CancellationToken ct = default)
    {
        var usage = await _db.CallUsages.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (usage is null) return Result<bool>.NotFound("Call reading");

        if (usage.IsBilled)
            return Result<bool>.Conflict("This reading has been billed and cannot be removed.");

        _db.CallUsages.Remove(usage);
        await _db.SaveChangesAsync(ct);
        return Result<bool>.Ok(true);
    }
}
