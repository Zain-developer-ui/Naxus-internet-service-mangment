using Microsoft.EntityFrameworkCore;
using NEXUS.Common;
using NEXUS.Data;
using NEXUS.Domain.Procurement;
using NEXUS.Models.ViewModels;

namespace NEXUS.Services.Feedback;

/**
 * Customer feedback. Previously a no-op, so the confirmation toast was the
 * only thing that ever happened. This writes the row and can read it back
 * for the customer's own history view.
 */
public interface IFeedbackService
{
    Task<Result<int>> SubmitAsync(int customerId, FeedbackViewModel model, CancellationToken ct = default);
    Task<Result<IReadOnlyList<FeedbackRecord>>> ForCustomerAsync(int customerId, CancellationToken ct = default);
}

public sealed record FeedbackRecord(int Id, int Rating, string Subject, string Message,
    bool IsResolved, string? Response, DateTime? RespondedAt, DateTime SubmittedAt);

public sealed class FeedbackService : IFeedbackService
{
    private readonly NexusDbContext _db;

    public FeedbackService(NexusDbContext db) => _db = db;

    public async Task<Result<int>> SubmitAsync(int customerId, FeedbackViewModel model,
                                               CancellationToken ct = default)
    {
        var customerExists = await _db.Customers
            .AsNoTracking()
            .AnyAsync(c => c.Id == customerId, ct);

        if (!customerExists) return Result<int>.NotFound("Customer");

        // The form carries four scores; the schema keeps one. The overall
        // rating is what the customer actually meant, so that is the one kept.
        var entry = new Domain.Procurement.Feedback
        {
            CustomerId = customerId,
            Rating = model.OverallRating,
            Subject = Truncate(model.Category, 100),
            Message = model.Message.Trim(),
            IsResolved = false
        };

        _db.Feedback.Add(entry);
        await _db.SaveChangesAsync(ct);

        return Result<int>.Ok(entry.Id);
    }

    public async Task<Result<IReadOnlyList<FeedbackRecord>>> ForCustomerAsync(
        int customerId, CancellationToken ct = default)
    {
        var rows = await _db.Feedback
            .AsNoTracking()
            .Where(f => f.CustomerId == customerId)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new FeedbackRecord(f.Id, f.Rating, f.Subject, f.Message,
                f.IsResolved, f.Response, f.RespondedAt, f.CreatedAt))
            .ToListAsync(ct);

        return Result<IReadOnlyList<FeedbackRecord>>.Ok(rows);
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}
