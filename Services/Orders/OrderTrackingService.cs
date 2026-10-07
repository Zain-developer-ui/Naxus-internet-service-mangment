using Microsoft.EntityFrameworkCore;
using NEXUS.Common;
using NEXUS.Common.Constants;
using NEXUS.Data;
using NEXUS.Domain.Orders;
using NEXUS.Models.ViewModels;

namespace NEXUS.Services.Orders;

/**
 * Public-facing order tracking. Replaces the mock ApiService for this route -
 * a customer enters the order reference from their receipt and sees where it
 * actually stands, plus the timeline of what has already happened.
 */
public interface IOrderTrackingService
{
    Task<Result<OrderTrackingViewModel>> GetAsync(string orderId, int? customerId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<OrderTrackingSummary>>> ForCustomerAsync(int customerId, CancellationToken ct = default);
}

public sealed class OrderTrackingService : IOrderTrackingService
{
    private readonly NexusDbContext _db;

    public OrderTrackingService(NexusDbContext db) => _db = db;

    public async Task<Result<OrderTrackingViewModel>> GetAsync(string orderId, int? customerId,
                                                               CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(orderId))
            return Result<OrderTrackingViewModel>.Fail(ErrorKind.Validation, "Enter an order reference to track.");

        var key = orderId.Trim().ToUpperInvariant();

        var order = await _db.ConnectionOrders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Plan)
            .Include(o => o.City)
            .Include(o => o.Feasibility)
            .FirstOrDefaultAsync(o => o.OrderId.ToUpper() == key, ct);

        if (order is null)
            return Result<OrderTrackingViewModel>.NotFound("Order");

        // A signed-in customer may only track their own orders; anyone else
        // reaching this with an id typed in gets a plain not-found so the
        // endpoint cannot be used to probe which references exist.
        if (customerId is int id && order.CustomerId != id)
            return Result<OrderTrackingViewModel>.NotFound("Order");

        var monthly = await _db.PlanPrices
            .AsNoTracking()
            .Where(p => p.PlanId == order.PlanId && p.Cycle == order.BillingCycle && p.IsAvailable)
            .Select(p => (decimal?)p.Amount)
            .FirstOrDefaultAsync(ct);

        return Result<OrderTrackingViewModel>.Ok(new OrderTrackingViewModel
        {
            OrderId = order.OrderId,
            CustomerName = order.Customer.FullName,
            ConnectionType = order.ConnectionType.DisplayName(),
            PlanName = order.Plan.Name,
            City = order.City.Name,
            Address = order.InstallationAddress,
            Status = order.Status.DisplayName(),
            StatusBadge = order.Status.BadgeClass(),
            PlacedAt = order.CreatedAt,
            ScheduledFor = order.ScheduledFor,
            MonthlyRate = monthly ?? order.QuotedAmount,
            SecurityDeposit = order.Plan.SecurityDeposit,
            Timeline = BuildTimeline(order)
        });
    }

    public async Task<Result<IReadOnlyList<OrderTrackingSummary>>> ForCustomerAsync(
        int customerId, CancellationToken ct = default)
    {
        var orders = await _db.ConnectionOrders
            .AsNoTracking()
            .Include(o => o.Plan)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new OrderTrackingSummary(
                o.OrderId,
                o.Plan.Name,
                o.ConnectionType.DisplayName(),
                o.Status.DisplayName(),
                o.Status.BadgeClass(),
                o.CreatedAt))
            .ToListAsync(ct);

        return Result<IReadOnlyList<OrderTrackingSummary>>.Ok(orders);
    }

    /**
     * The four SRS stages, each resolved against the order's real status.
     * An order that was cancelled or failed feasibility stops the ladder at
     * the point it broke rather than pretending the later steps are pending.
     */
    private static IReadOnlyList<TrackingStep> BuildTimeline(ConnectionOrder order)
    {
        var stage = order.Status switch
        {
            OrderStatus.Draft => 0,
            OrderStatus.AwaitingFeasibility => 1,
            OrderStatus.FeasibilityPassed or OrderStatus.FeasibilityFailed => 2,
            OrderStatus.Confirmed => 2,
            OrderStatus.Installing => 3,
            OrderStatus.Completed => 4,
            OrderStatus.Cancelled => 1,
            _ => 0
        };

        var stopped = order.Status is OrderStatus.Cancelled or OrderStatus.FeasibilityFailed;

        var steps = new List<TrackingStep>
        {
            new("Order Received", "Your application is on file and waiting to be surveyed.", "done"),
            new("Feasibility Check",
                order.Feasibility is null
                    ? "Our team still has to verify coverage at your address."
                    : $"Survey recorded - {order.Feasibility.ResultLabel.ToLowerInvariant()}.",
                State(1)),
            new("Installation Scheduled",
                order.ScheduledFor is DateTime when && order.Status != OrderStatus.Cancelled
                    ? $"Booked for {when:dd MMM yyyy}."
                    : "We will contact you to agree a slot.",
                State(2)),
            new("Connection Activated",
                order.CompletedAt is DateTime done
                    ? $"Line went live on {done:dd MMM yyyy}."
                    : "You will be notified once the line is live.",
                State(3))
        };

        if (stopped)
        {
            steps[1] = steps[1] with
            {
                Description = order.Status == OrderStatus.Cancelled
                    ? "This order was cancelled."
                    : "Coverage at this address did not pass the survey.",
                State = "failed"
            };
        }

        return steps;

        string State(int index) =>
            stopped ? "" :
            index < stage ? "done" :
            index == stage ? "active" : "";
    }
}

public sealed record OrderTrackingSummary(string OrderId, string PlanName, string ConnectionType,
    string Status, string StatusBadge, DateTime PlacedAt);
