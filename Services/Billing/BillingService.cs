using Microsoft.EntityFrameworkCore;
using NEXUS.Common;
using NEXUS.Common.Abstractions;
using NEXUS.Common.Billing;
using NEXUS.Common.Constants;
using NEXUS.Data;
using NEXUS.Domain.Billing;
using NEXUS.Domain.Catalog;
using NEXUS.Domain.Customers;
using NEXUS.Domain.Orders;
using NEXUS.Services.Generation;

namespace NEXUS.Services.Billing;

/** A bill as the UI needs it, with the customer and connection context attached. */
public sealed record BillSummary(
    int Id,
    string BillNumber,
    string AccountId,
    string CustomerName,
    string? ConnectionNumber,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    DateTime IssuedOn,
    DateTime DueOn,
    BillStatus Status,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxableAmount,
    decimal ServiceTaxAmount,
    decimal TotalAmount,
    decimal AmountPaid)
{
    public decimal OutstandingAmount => TotalAmount - AmountPaid;
    public bool IsOverdue => Status == BillStatus.Overdue;
    public string StatusLabel => Status.DisplayName();
    public string StatusBadge => Status.BadgeClass();
}

public sealed record BillLineView(
    string Description,
    string ChargeType,
    decimal Quantity,
    decimal UnitAmount,
    decimal LineAmount);

public sealed record BillDetail(
    BillSummary Bill,
    string CustomerName,
    string CustomerAddress,
    string City,
    string? OrganisationName,
    IReadOnlyList<BillLineView> Lines);

public sealed record BillDraft(
    int CustomerId,
    int ConnectionId,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    IReadOnlyList<ChargeLine> Lines);

public sealed record PaymentRecord(
    int Id,
    string ReceiptNumber,
    decimal Amount,
    PaymentMethod Method,
    DateTime PaidOn,
    string? Reference,
    string? Remarks);

public sealed record PaymentRecorded(BillSummary Bill, PaymentRecord Payment);

/** What a generation run did, so the UI can report honestly. */
public sealed record GenerationOutcome(int Created, int Skipped, IReadOnlyList<string> SkippedReasons);

public interface IBillingService
{
    Task<Result<BillDetail>> GetAsync(int billId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<BillSummary>>> ListAsync(BillQuery query, CancellationToken ct = default);
    Task<Result<IReadOnlyList<BillSummary>>> ForCustomerAsync(int customerId, CancellationToken ct = default);

    Task<Result<BillDraft>> PrepareDraftAsync(int customerId, int connectionId,
                                              DateTime periodStart, DateTime periodEnd,
                                              CancellationToken ct = default);

    Task<Result<BillDetail>> CreateAsync(BillDraft draft, CancellationToken ct = default);
    Task<Result<GenerationOutcome>> GenerateForPeriodAsync(DateTime periodStart, DateTime periodEnd,
                                                           CancellationToken ct = default);

    Task<Result<BillDetail>> IssueAsync(int billId, CancellationToken ct = default);
    Task<Result<BillDetail>> CancelAsync(int billId, string reason, CancellationToken ct = default);

    Task<Result<PaymentRecorded>> RecordPaymentAsync(int billId, decimal amount,
                                                     PaymentMethod method, string? reference,
                                                     string? remarks, int? shopId, int? userId,
                                                     CancellationToken ct = default);

    Task<Result<IReadOnlyList<PaymentRecord>>> PaymentsForAsync(int billId, CancellationToken ct = default);
}

public sealed record BillQuery(
    int? CustomerId = null,
    BillStatus? Status = null,
    DateTime? IssuedFrom = null,
    DateTime? IssuedTo = null,
    string? Search = null,
    bool OverdueOnly = false,
    int Take = 100);

/**
 * The billing engine.
 *
 * Three things shape this class:
 *   - Bills are only ever created by the accounts department, so nothing here
 *     is reachable from a customer-facing route.
 *   - The SRS forbids billing a line that is not Active (rule V9), and a
 *     temporarily inactive line still owes nothing for the inactive window.
 *   - Money arithmetic runs through BillCalculator, which owns the discount
 *     and tax order, so the figures on screen and the figures stored agree.
 */
public sealed class BillingService : IBillingService
{
    private const int DueDays = 15;

    private readonly NexusDbContext _db;
    private readonly IAccountIdGenerator _ids;
    private readonly ILogger<BillingService> _log;

    public BillingService(NexusDbContext db, IAccountIdGenerator ids, ILogger<BillingService> log)
    {
        _db = db;
        _ids = ids;
        _log = log;
    }

    public async Task<Result<BillDetail>> GetAsync(int billId, CancellationToken ct = default)
    {
        var bill = await _db.Bills
            .AsNoTracking()
            .Include(b => b.Customer).ThenInclude(c => c.City)
            .Include(b => b.Connection)
            .Include(b => b.Lines)
            .FirstOrDefaultAsync(b => b.Id == billId, ct);

        return bill is null ? Result<BillDetail>.NotFound("Bill") : Result<BillDetail>.Ok(ToDetail(bill));
    }

    public async Task<Result<IReadOnlyList<BillSummary>>> ListAsync(BillQuery query,
                                                                    CancellationToken ct = default)
    {
        var q = _db.Bills
            .AsNoTracking()
            .Include(b => b.Customer)
            .Include(b => b.Connection)
            .AsQueryable();

        if (query.CustomerId is { } customerId)
            q = q.Where(b => b.CustomerId == customerId);

        if (query.Status is { } status)
            q = q.Where(b => b.Status == status);

        if (query.IssuedFrom is { } from)
            q = q.Where(b => b.IssuedOn >= from.Date);

        if (query.IssuedTo is { } to)
            q = q.Where(b => b.IssuedOn < to.Date.AddDays(1));

        if (query.OverdueOnly)
            q = q.Where(b => b.Status == BillStatus.Overdue ||
                             (b.Status != BillStatus.Paid && b.Status != BillStatus.Cancelled &&
                              b.DueOn < DateTime.UtcNow.Date));

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(b => b.BillNumber.Contains(term) ||
                             b.Customer.AccountId.Contains(term) ||
                             b.Customer.FullName.Contains(term));
        }

        var bills = await q
            .OrderByDescending(b => b.IssuedOn)
            .ThenByDescending(b => b.Id)
            .Take(Math.Clamp(query.Take, 1, 500))
            .ToListAsync(ct);

        return Result<IReadOnlyList<BillSummary>>.Ok(bills.Select(ToSummary).ToList());
    }

    public async Task<Result<IReadOnlyList<BillSummary>>> ForCustomerAsync(int customerId,
                                                                          CancellationToken ct = default)
    {
        var bills = await _db.Bills
            .AsNoTracking()
            .Include(b => b.Customer)
            .Include(b => b.Connection)
            .Where(b => b.CustomerId == customerId)
            .OrderByDescending(b => b.PeriodStart)
            .ThenByDescending(b => b.Id)
            .ToListAsync(ct);

        return Result<IReadOnlyList<BillSummary>>.Ok(bills.Select(ToSummary).ToList());
    }

    /**
     * Builds the lines for a bill without writing anything. The UI shows this
     * as a preview so accounts staff can see the discount and tax before
     * committing, and CreateAsync recomputes from the same code path rather
     * than trusting whatever the preview returned.
     */
    public async Task<Result<BillDraft>> PrepareDraftAsync(int customerId, int connectionId,
                                                           DateTime periodStart, DateTime periodEnd,
                                                           CancellationToken ct = default)
    {
        if (periodEnd <= periodStart)
            return Result<BillDraft>.Fail(nameof(periodEnd), "The period end must be after its start.");

        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.Id == customerId, ct);

        if (customer is null) return Result<BillDraft>.NotFound("Customer");

        var connection = await _db.Connections
            .Include(c => c.Plan).ThenInclude(p => p.Prices)
            .FirstOrDefaultAsync(c => c.Id == connectionId && c.CustomerId == customerId, ct);

        if (connection is null)
            return Result<BillDraft>.NotFound("Connection");

        if (connection.IsDeleted)
            return Result<BillDraft>.Conflict("That connection has been removed and cannot be billed.");

        if (!connection.Status.CanBeBilled())
        {
            return Result<BillDraft>.Conflict(
                $"{connection.ConnectionNumber} is {connection.Status.DisplayName()} - the SRS only bills active lines.");
        }

        if (await HasBillForPeriodAsync(connectionId, periodStart, periodEnd, ct))
        {
            return Result<BillDraft>.Conflict(
                $"A bill for {connection.ConnectionNumber} already covers this period.");
        }

        var lines = BuildLines(connection, periodStart, periodEnd);
        if (lines.Count == 0)
            return Result<BillDraft>.Conflict("This plan has no rate configured for the selected period.");

        return Result<BillDraft>.Ok(new BillDraft(customerId, connectionId, periodStart, periodEnd, lines));
    }

    public async Task<Result<BillDetail>> CreateAsync(BillDraft draft, CancellationToken ct = default)
    {
        var customer = await _db.Customers
            .Include(c => c.Connections)
            .FirstOrDefaultAsync(c => c.Id == draft.CustomerId, ct);

        if (customer is null) return Result<BillDetail>.NotFound("Customer");

        if (draft.Lines.Count == 0)
            return Result<BillDetail>.Fail(nameof(draft.Lines), "A bill needs at least one charge.");

        var connection = await _db.Connections
            .FirstOrDefaultAsync(c => c.Id == draft.ConnectionId &&
                                      c.CustomerId == draft.CustomerId, ct);

        if (connection is null) return Result<BillDetail>.NotFound("Connection");

        var breakdown = BillCalculator.Compute(draft.Lines, customer.LiveConnectionCount);

        var bill = new Bill
        {
            CustomerId = customer.Id,
            ConnectionId = connection.Id,
            PeriodStart = draft.PeriodStart.Date,
            PeriodEnd = draft.PeriodEnd.Date,
            IssuedOn = DateTime.UtcNow.Date,
            DueOn = DateTime.UtcNow.Date.AddDays(DueDays),
            Status = BillStatus.Draft,
            SubTotal = breakdown.SubTotal,
            DiscountAmount = breakdown.DiscountAmount,
            TaxableAmount = breakdown.TaxableAmount,
            ServiceTaxAmount = breakdown.ServiceTaxAmount,
            TotalAmount = breakdown.TotalAmount,
            AmountPaid = 0m
        };

        var sort = 0;
        foreach (var line in breakdown.Lines)
        {
            bill.Lines.Add(new BillLine
            {
                Description = line.Description,
                ChargeType = line.ChargeType,
                Quantity = line.Quantity,
                UnitAmount = line.UnitAmount,
                LineAmount = line.LineAmount,
                SortOrder = sort++
            });
        }

        if (breakdown.HasDiscount)
        {
            bill.Lines.Add(new BillLine
            {
                Description = $"Bulk discount - {breakdown.Tier!.Label} at {breakdown.DiscountRate:P0}",
                ChargeType = ChargeTypes.Adjustment,
                Quantity = 1m,
                UnitAmount = 0m,
                LineAmount = 0m,
                SortOrder = sort
            });
        }

        var strategy = _db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            bill.BillNumber = await _ids.NextBillNumberAsync(
                new DateOnly(draft.PeriodStart.Year, draft.PeriodStart.Month, 1), ct);

            _db.Bills.Add(bill);
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        });

        _log.LogInformation("Bill {BillNumber} drafted for customer {AccountId} totalling {Total}",
            bill.BillNumber, customer.AccountId, bill.TotalAmount);

        return await GetAsync(bill.Id, ct);
    }

    /**
     * Runs the monthly cycle over every billable connection. Connections that
     * already have a bill for the window are skipped rather than duplicated,
     * and the reasons are returned so the caller can show what was left out.
     */
    public async Task<Result<GenerationOutcome>> GenerateForPeriodAsync(DateTime periodStart,
                                                                        DateTime periodEnd,
                                                                        CancellationToken ct = default)
    {
        if (periodEnd <= periodStart)
            return Result<GenerationOutcome>.Fail(nameof(periodEnd), "The period end must be after its start.");

        var connections = await _db.Connections
            .Include(c => c.Customer).ThenInclude(cu => cu.Connections)
            .Include(c => c.Plan).ThenInclude(p => p.Prices)
            .Where(c => !c.IsDeleted && c.Status == ConnectionStatus.Active)
            .ToListAsync(ct);

        var created = 0;
        var skipped = 0;
        var reasons = new List<string>();

        foreach (var connection in connections)
        {
            ct.ThrowIfCancellationRequested();

            if (await HasBillForPeriodAsync(connection.Id, periodStart, periodEnd, ct))
            {
                skipped++;
                reasons.Add($"{connection.ConnectionNumber}: already billed for this period.");
                continue;
            }

            var lines = BuildLines(connection, periodStart, periodEnd);
            if (lines.Count == 0)
            {
                skipped++;
                reasons.Add($"{connection.ConnectionNumber}: no rate configured for this period.");
                continue;
            }

            var customer = connection.Customer;
            var breakdown = BillCalculator.Compute(lines, customer.LiveConnectionCount);

            var bill = new Bill
            {
                CustomerId = customer.Id,
                ConnectionId = connection.Id,
                PeriodStart = periodStart.Date,
                PeriodEnd = periodEnd.Date,
                IssuedOn = DateTime.UtcNow.Date,
                DueOn = DateTime.UtcNow.Date.AddDays(DueDays),
                Status = BillStatus.Draft,
                SubTotal = breakdown.SubTotal,
                DiscountAmount = breakdown.DiscountAmount,
                TaxableAmount = breakdown.TaxableAmount,
                ServiceTaxAmount = breakdown.ServiceTaxAmount,
                TotalAmount = breakdown.TotalAmount
            };

            var sort = 0;
            foreach (var line in breakdown.Lines)
            {
                bill.Lines.Add(new BillLine
                {
                    Description = line.Description,
                    ChargeType = line.ChargeType,
                    Quantity = line.Quantity,
                    UnitAmount = line.UnitAmount,
                    LineAmount = line.LineAmount,
                    SortOrder = sort++
                });
            }

            var strategy = _db.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _db.Database.BeginTransactionAsync(ct);
                bill.BillNumber = await _ids.NextBillNumberAsync(
                    new DateOnly(periodStart.Year, periodStart.Month, 1), ct);
                _db.Bills.Add(bill);
                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            });

            created++;
        }

        _log.LogInformation("Billing run for {Start:d}-{End:d}: {Created} drafted, {Skipped} skipped",
            periodStart, periodEnd, created, skipped);

        return Result<GenerationOutcome>.Ok(new GenerationOutcome(created, skipped, reasons));
    }

    public async Task<Result<BillDetail>> IssueAsync(int billId, CancellationToken ct = default)
    {
        var bill = await _db.Bills.FirstOrDefaultAsync(b => b.Id == billId, ct);
        if (bill is null) return Result<BillDetail>.NotFound("Bill");

        if (bill.Status != BillStatus.Draft)
            return Result<BillDetail>.Conflict($"Only a draft can be issued. This bill is {bill.Status.DisplayName()}.");

        if (bill.Lines.Count == 0 && !await _db.BillLines.AnyAsync(l => l.BillId == billId, ct))
            return Result<BillDetail>.Conflict("A bill needs at least one charge before it can be issued.");

        var strategy = _db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            var tracked = await _db.Bills.Include(b => b.Lines).FirstAsync(b => b.Id == billId, ct);
            tracked.Status = BillStatus.Issued;
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        });

        return await GetAsync(billId, ct);
    }

    public async Task<Result<BillDetail>> CancelAsync(int billId, string reason,
                                                      CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Result<BillDetail>.Fail(nameof(reason), "Give a reason for cancelling the bill.");

        var bill = await _db.Bills.FirstOrDefaultAsync(b => b.Id == billId, ct);
        if (bill is null) return Result<BillDetail>.NotFound("Bill");

        if (bill.Status == BillStatus.Paid)
            return Result<BillDetail>.Conflict("A paid bill cannot be cancelled. Raise a credit note instead.");

        if (bill.AmountPaid > 0m)
            return Result<BillDetail>.Conflict("This bill has payments against it. Refund them before cancelling.");

        bill.Status = BillStatus.Cancelled;
        bill.Notes = string.IsNullOrWhiteSpace(bill.Notes)
            ? $"Cancelled: {reason.Trim()}"
            : $"{bill.Notes} | Cancelled: {reason.Trim()}";

        await _db.SaveChangesAsync(ct);
        return await GetAsync(billId, ct);
    }

    /**
     * Records money against a bill. Both the accounts desk and a retail outlet
     * can take payment, which is why the channel is stored.
     *
     * The bill status is derived from the running total rather than set by the
     * caller - a partial payment leaves it PartiallyPaid, and the final one
     * closes it. Overpayment is rejected rather than silently swallowed.
     */
    public async Task<Result<PaymentRecorded>> RecordPaymentAsync(int billId, decimal amount,
                                                                  PaymentMethod method, string? reference,
                                                                  string? remarks, int? shopId, int? userId,
                                                                  CancellationToken ct = default)
    {
        if (amount <= 0m)
            return Result<PaymentRecorded>.Fail(nameof(amount), "Enter an amount greater than zero.");

        if (method == PaymentMethod.Cheque && string.IsNullOrWhiteSpace(reference))
            return Result<PaymentRecorded>.Fail(nameof(reference), "A cheque payment needs a cheque number.");

        var bill = await _db.Bills
            .Include(b => b.Customer)
            .Include(b => b.Connection)
            .FirstOrDefaultAsync(b => b.Id == billId, ct);

        if (bill is null) return Result<PaymentRecorded>.NotFound("Bill");

        if (bill.Status == BillStatus.Cancelled)
            return Result<PaymentRecorded>.Conflict("This bill is cancelled and cannot take payment.");

        if (bill.Status == BillStatus.Draft)
            return Result<PaymentRecorded>.Conflict("Issue the bill before recording a payment against it.");

        var outstanding = bill.OutstandingAmount;
        if (outstanding <= 0m)
            return Result<PaymentRecorded>.Conflict("This bill is already settled.");

        if (amount > outstanding)
        {
            return Result<PaymentRecorded>.Fail(nameof(amount),
                $"That is more than the outstanding balance of {outstanding:C}.");
        }

        var payment = new Payment
        {
            BillId = bill.Id,
            CustomerId = bill.CustomerId,
            Amount = Math.Round(amount, 2),
            Method = method,
            PaidOn = DateTime.UtcNow,
            Reference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim(),
            Remarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim(),
            ReceivedByShopId = shopId,
            ReceivedByUserId = userId
        };

        var strategy = _db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            payment.ReceiptNumber = await _ids.NextReceiptNumberAsync(ct);

            var tracked = await _db.Bills.FirstAsync(b => b.Id == billId, ct);
            tracked.AmountPaid = Math.Round(tracked.AmountPaid + payment.Amount, 2);
            tracked.Status = tracked.AmountPaid >= tracked.TotalAmount
                ? BillStatus.Paid
                : BillStatus.PartiallyPaid;

            _db.Payments.Add(payment);
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        });

        _log.LogInformation("Payment {Receipt} of {Amount} recorded against {BillNumber}",
            payment.ReceiptNumber, payment.Amount, bill.BillNumber);

        var refreshed = await GetAsync(billId, ct);
        if (!refreshed.IsSuccess) return Result<PaymentRecorded>.NotFound("Bill");

        var record = new PaymentRecord(payment.Id, payment.ReceiptNumber, payment.Amount,
                                       payment.Method, payment.PaidOn, payment.Reference, payment.Remarks);

        return Result<PaymentRecorded>.Ok(new PaymentRecorded(refreshed.Value!.Bill, record));
    }

    public async Task<Result<IReadOnlyList<PaymentRecord>>> PaymentsForAsync(int billId,
                                                                             CancellationToken ct = default)
    {
        if (!await _db.Bills.AnyAsync(b => b.Id == billId, ct))
            return Result<IReadOnlyList<PaymentRecord>>.NotFound("Bill");

        var payments = await _db.Payments
            .AsNoTracking()
            .Where(p => p.BillId == billId)
            .OrderBy(p => p.PaidOn)
            .ToListAsync(ct);

        return Result<IReadOnlyList<PaymentRecord>>.Ok(
            payments.Select(p => new PaymentRecord(p.Id, p.ReceiptNumber, p.Amount, p.Method,
                                                   p.PaidOn, p.Reference, p.Remarks))
                    .ToList());
    }

    /**
     * Rental for the window, priced off the cycle the customer signed up for.
     * An hourly plan that is unlimited produces no usage line; a metered plan
     * has nothing to measure here because the SRS tracks nothing per period,
     * so usage only appears when accounts enters it.
     */
    private static List<ChargeLine> BuildLines(Connection connection, DateTime periodStart,
                                               DateTime periodEnd)
    {
        var lines = new List<ChargeLine>();
        var plan = connection.Plan;

        var price = plan.Prices.FirstOrDefault(p => p.Cycle == BillingCycle.Monthly && p.IsAvailable)
                    ?? plan.Prices.FirstOrDefault(p => p.IsAvailable);

        if (price is null) return lines;

        var monthlyRate = price.EffectiveMonthlyRate;
        if (monthlyRate <= 0m) return lines;

        lines.Add(BillCalculator.RentalLine(plan.DisplayName, monthlyRate,
                                            BillingCycle.Monthly, periodStart, periodEnd));

        return lines;
    }

    private Task<bool> HasBillForPeriodAsync(int connectionId, DateTime periodStart,
                                             DateTime periodEnd, CancellationToken ct) =>
        _db.Bills.AnyAsync(b => b.ConnectionId == connectionId &&
                                b.Status != BillStatus.Cancelled &&
                                b.PeriodStart < periodEnd.Date &&
                                b.PeriodEnd > periodStart.Date, ct);

    private static BillSummary ToSummary(Bill bill) => new(
        bill.Id,
        bill.BillNumber,
        bill.Customer?.AccountId ?? string.Empty,
        bill.Customer?.FullName ?? string.Empty,
        bill.Connection?.ConnectionNumber,
        bill.PeriodStart,
        bill.PeriodEnd,
        bill.IssuedOn,
        bill.DueOn,
        bill.Status,
        bill.SubTotal,
        bill.DiscountAmount,
        bill.TaxableAmount,
        bill.ServiceTaxAmount,
        bill.TotalAmount,
        bill.AmountPaid);

    private static BillDetail ToDetail(Bill bill) => new(
        ToSummary(bill),
        bill.Customer?.FullName ?? string.Empty,
        bill.Customer?.Address ?? string.Empty,
        bill.Customer?.City?.Name ?? string.Empty,
        bill.Customer?.OrganisationName,
        bill.Lines.OrderBy(l => l.SortOrder)
                   .Select(l => new BillLineView(l.Description, l.ChargeType, l.Quantity,
                                                 l.UnitAmount, l.LineAmount))
                   .ToList());
}
