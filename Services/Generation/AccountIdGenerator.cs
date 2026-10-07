using Microsoft.EntityFrameworkCore;
using NEXUS.Common.Constants;
using NEXUS.Data;

namespace NEXUS.Services.Generation;

public interface IAccountIdGenerator
{
    Task<string> NextAccountIdAsync(ConnectionType type, int cityCode, CancellationToken ct = default);
    Task<string> NextOrderIdAsync(ConnectionType type, CancellationToken ct = default);
    Task<string> NextBillNumberAsync(DateOnly period, CancellationToken ct = default);
    Task<string> NextReceiptNumberAsync(CancellationToken ct = default);
    Task<string> NextConnectionNumberAsync(ConnectionType type, CancellationToken ct = default);
}

/**
 * Identifiers are derived from the SRS formats, so they carry meaning:
 * the prefix letter is the connection type and the city code is embedded.
 *
 * Two counters can be handed out at the same moment - two retail clerks
 * registering customers, two accounts staff generating bills - so each
 * generator reads the current maximum inside the surrounding transaction and
 * retries on a unique-index collision rather than trusting a local counter.
 */
public sealed class AccountIdGenerator : IAccountIdGenerator
{
    private const int MaxAttempts = 5;

    private readonly NexusDbContext _db;

    public AccountIdGenerator(NexusDbContext db) => _db = db;

    public async Task<string> NextAccountIdAsync(ConnectionType type, int cityCode,
                                                 CancellationToken ct = default)
    {
        var prefix = type.OrderPrefix();
        var serialLength = IdFormats.AccountSerialLength;

        return await GenerateAsync(
            async () =>
            {
                var existing = await _db.Customers
                    .IgnoreQueryFilters()
                    .Where(c => c.AccountId.StartsWith(prefix.ToString()))
                    .Select(c => c.AccountId)
                    .ToListAsync(ct);

                var maxSerial = existing
                    .Where(IdFormats.IsWellFormedAccountId)
                    .Select(id => long.TryParse(id[^serialLength..], out var n) ? n : 0)
                    .DefaultIfEmpty(0)
                    .Max();

                return IdFormats.BuildAccountId(prefix, cityCode, maxSerial + 1);
            },
            candidate => _db.Customers.IgnoreQueryFilters()
                                 .AnyAsync(c => c.AccountId == candidate, ct),
            ct);
    }

    public async Task<string> NextOrderIdAsync(ConnectionType type, CancellationToken ct = default)
    {
        var prefix = type.OrderPrefix();

        return await GenerateAsync(
            async () =>
            {
                var existing = await _db.ConnectionOrders
                    .Where(o => o.OrderId.StartsWith(prefix.ToString()))
                    .Select(o => o.OrderId)
                    .ToListAsync(ct);

                var maxSerial = existing
                    .Where(IdFormats.IsWellFormedOrderId)
                    .Select(id => int.TryParse(id[1..], out var n) ? n : 0)
                    .DefaultIfEmpty(0)
                    .Max();

                return IdFormats.BuildOrderId(prefix, maxSerial + 1);
            },
            candidate => _db.ConnectionOrders.AnyAsync(o => o.OrderId == candidate, ct),
            ct);
    }

    public async Task<string> NextBillNumberAsync(DateOnly period, CancellationToken ct = default)
    {
        var prefix = $"INV-{period.Year}-";

        return await GenerateAsync(
            async () =>
            {
                var existing = await _db.Bills
                    .Where(b => b.BillNumber.StartsWith(prefix))
                    .Select(b => b.BillNumber)
                    .ToListAsync(ct);

                var maxSerial = existing
                    .Select(n => int.TryParse(n[prefix.Length..], out var v) ? v : 0)
                    .DefaultIfEmpty(0)
                    .Max();

                return $"{prefix}{maxSerial + 1:D5}";
            },
            candidate => _db.Bills.AnyAsync(b => b.BillNumber == candidate, ct),
            ct);
    }

    public async Task<string> NextReceiptNumberAsync(CancellationToken ct = default)
    {
        var prefix = $"RCP-{DateTime.UtcNow:yyyyMM}-";

        return await GenerateAsync(
            async () =>
            {
                var existing = await _db.Payments
                    .Where(p => p.ReceiptNumber.StartsWith(prefix))
                    .Select(p => p.ReceiptNumber)
                    .ToListAsync(ct);

                var maxSerial = existing
                    .Select(n => int.TryParse(n[prefix.Length..], out var v) ? v : 0)
                    .DefaultIfEmpty(0)
                    .Max();

                return $"{prefix}{maxSerial + 1:D5}";
            },
            candidate => _db.Payments.AnyAsync(p => p.ReceiptNumber == candidate, ct),
            ct);
    }

    public async Task<string> NextConnectionNumberAsync(ConnectionType type, CancellationToken ct = default)
    {
        var prefix = $"CON-{type.OrderPrefix()}-";

        return await GenerateAsync(
            async () =>
            {
                var existing = await _db.Connections
                    .IgnoreQueryFilters()
                    .Where(c => c.ConnectionNumber.StartsWith(prefix))
                    .Select(c => c.ConnectionNumber)
                    .ToListAsync(ct);

                var maxSerial = existing
                    .Select(n => int.TryParse(n[prefix.Length..], out var v) ? v : 0)
                    .DefaultIfEmpty(0)
                    .Max();

                return $"{prefix}{maxSerial + 1:D6}";
            },
            candidate => _db.Connections.IgnoreQueryFilters()
                                 .AnyAsync(c => c.ConnectionNumber == candidate, ct),
            ct);
    }

    /**
     * Reads the maximum, proposes the next value, and if the unique index
     * rejects it because someone else won the race, reads again. Bounded so a
     * persistent failure surfaces instead of spinning forever.
     */
    private static async Task<string> GenerateAsync(Func<Task<string>> propose,
                                                    Func<string, Task<bool>> exists,
                                                    CancellationToken ct)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            var candidate = await propose();
            if (!await exists(candidate)) return candidate;
        }

        throw new InvalidOperationException(
            "Could not allocate a unique identifier after repeated attempts.");
    }
}
