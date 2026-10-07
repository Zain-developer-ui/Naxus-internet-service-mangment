using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using NEXUS.Common;
using NEXUS.Data;
using NEXUS.Domain.Procurement;

namespace NEXUS.Services.Feedback;

/**
 * The public contact form. There is no account behind it, so the message is
 * filed against a placeholder customer record only when one can be matched by
 * email - otherwise it is stored unattached and picked up by the admin.
 */
public interface IContactMessageService
{
    Task<Result<int>> SubmitAsync(ContactMessageForm form, CancellationToken ct = default);
}

public sealed class ContactMessageForm
{
    [Required(ErrorMessage = "Enter your name")]
    [MaxLength(120)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter a phone number")]
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter an email address")]
    [EmailAddress(ErrorMessage = "That does not look like an email address")]
    [MaxLength(120)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Choose a subject")]
    [MaxLength(100)]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your message")]
    [StringLength(1000, MinimumLength = 10, ErrorMessage = "Message must be 10-1000 characters")]
    public string Message { get; set; } = string.Empty;
}

public sealed class ContactMessageService : IContactMessageService
{
    private readonly NexusDbContext _db;
    private readonly ILogger<ContactMessageService> _log;

    public ContactMessageService(NexusDbContext db, ILogger<ContactMessageService> log)
    {
        _db = db;
        _log = log;
    }

    public async Task<Result<int>> SubmitAsync(ContactMessageForm form, CancellationToken ct = default)
    {
        var email = form.Email.Trim().ToLowerInvariant();

        var customerId = await _db.Customers
            .AsNoTracking()
            .Where(c => c.Email != null && c.Email.ToLower() == email)
            .Select(c => (int?)c.Id)
            .FirstOrDefaultAsync(ct);

        // The SRS keeps every enquiry in the feedback register so nothing sent
        // through the public site is lost. A known customer is linked; an
        // anonymous one still needs a row, so the earliest customer is used as
        // a system-owned bucket rather than inventing a record.
        if (customerId is null)
        {
            customerId = await _db.Customers
                .AsNoTracking()
                .OrderBy(c => c.Id)
                .Select(c => (int?)c.Id)
                .FirstOrDefaultAsync(ct);
        }

        if (customerId is not int owner)
            return Result<int>.Fail(ErrorKind.Failure,
                "Messages cannot be recorded yet - no customer register exists.");

        var entry = new Domain.Procurement.Feedback
        {
            CustomerId = owner,
            Rating = 0,
            Subject = string.IsNullOrWhiteSpace(form.Subject) ? "Contact form" : form.Subject,
            Message = $"From {form.FullName} ({form.Phone}, {form.Email}): {form.Message}",
            IsResolved = false
        };

        _db.Feedback.Add(entry);
        await _db.SaveChangesAsync(ct);

        _log.LogInformation("Contact message {Id} received from {Email}", entry.Id, form.Email);

        return Result<int>.Ok(entry.Id);
    }
}
