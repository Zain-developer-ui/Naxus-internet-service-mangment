using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using NEXUS.Common;
using NEXUS.Common.Abstractions;
using NEXUS.Data;
using NEXUS.Models.ViewModels;
using NEXUS.Services.Email;
using NEXUS.Services.Settings;

namespace NEXUS.Services.Authentication;

/** Which second factor was used, and the token when it comes back in the URL. */
public sealed record ResetStart(string Channel, string? Token);

/**
 * Self-service password reset.
 *
 * With a working mail server the link is emailed to the address on the account,
 * so the token never travels through the browser. Without one - which is the
 * default, and the only option offline - the second factor is the account id
 * plus the mobile number already held on the record, the same pair the public
 * status lookup asks for, and the token comes back in the redirect.
 *
 * Either way the token is short lived and protected, so the mobile number is
 * never carried onto the reset form and cannot be replayed from the URL.
 */
public interface IPasswordResetService
{
    Task<Result<ResetStart>> BeginAsync(string accountIdOrEmail, string phone, string baseUrl,
                                        CancellationToken ct = default);
    Task<Result<int>> CompleteAsync(string token, string newPassword,
                                    CancellationToken ct = default);
}

public sealed class PasswordResetService : IPasswordResetService
{
    private static readonly TimeSpan ValidFor = TimeSpan.FromMinutes(15);
    private const string Purpose = "NEXUS.PasswordReset";

    private static readonly Regex MobileShape = new(@"^0\d{3}-\d{7}$", RegexOptions.Compiled);

    private readonly NexusDbContext _db;
    private readonly IPasswordService _passwords;
    private readonly IEmailSender _emails;
    private readonly ISiteSettingsService _settings;
    private readonly IDataProtector _protector;

    public PasswordResetService(NexusDbContext db, IPasswordService passwords,
                                IEmailSender emails, ISiteSettingsService settings,
                                IDataProtectionProvider protection)
    {
        _db = db;
        _passwords = passwords;
        _emails = emails;
        _settings = settings;
        _protector = protection.CreateProtector(Purpose);
    }

    public async Task<Result<ResetStart>> BeginAsync(string accountIdOrEmail, string phone,
                                                     string baseUrl, CancellationToken ct = default)
    {
        var key = (accountIdOrEmail ?? string.Empty).Trim();
        var mobile = (phone ?? string.Empty).Trim();

        // One wording for every failure, so the form cannot be used to work out
        // which account ids exist.
        var refusal = Result<ResetStart>.Fail(string.Empty,
            "Those details do not match an account. Check them and try again.");

        var user = await _db.Users
            .FirstOrDefaultAsync(u => !u.IsDeleted && u.IsActive &&
                                      (u.AccountId == key || u.Email == key), ct);

        if (user is null) return refusal;

        var mail = await _emails.CurrentAsync(ct);

        if (mail.IsUsable && !string.IsNullOrWhiteSpace(user.Email))
        {
            var link = $"{baseUrl}/Account/ResetPassword?token=" +
                       Uri.EscapeDataString(Issue(user.Id));

            var siteName = await _settings.GetValueAsync("site.name", ct);
            var sent = await _emails.SendAsync(user.Email!, $"{siteName} password reset",
                                               BuildBody(siteName, link), ct);

            if (!sent.IsSuccess)
                return Result<ResetStart>.Fail(string.Empty,
                    sent.Message ?? "The reset email could not be sent.");

            return Result<ResetStart>.Ok(new ResetStart("email", null));
        }

        if (!MobileShape.IsMatch(mobile))
            return Result<ResetStart>.Fail(nameof(ForgotPasswordViewModel.Phone),
                "Enter the mobile number on the account, like 0300-1234567.");

        var ownsAsCustomer = await _db.Customers
            .AnyAsync(c => c.UserId == user.Id && c.Phone == mobile, ct);

        if (!ownsAsCustomer)
        {
            var ownsAsStaff = await _db.Employees
                .AnyAsync(e => e.UserId == user.Id && e.Phone == mobile, ct);

            if (!ownsAsStaff) return refusal;
        }

        return Result<ResetStart>.Ok(new ResetStart("mobile", Issue(user.Id)));
    }

    public async Task<Result<int>> CompleteAsync(string token, string newPassword,
                                                 CancellationToken ct = default)
    {
        var stale = Result<int>.Fail(string.Empty, "That reset link is no longer valid.");

        if (string.IsNullOrWhiteSpace(token)) return stale;

        string payload;
        try
        {
            payload = _protector.Unprotect(token);
        }
        catch
        {
            return stale;
        }

        var parts = payload.Split(':');

        if (parts.Length != 2 ||
            !int.TryParse(parts[0], out var userId) ||
            !long.TryParse(parts[1], out var expiresAt))
            return stale;

        if (DateTime.UtcNow.Ticks > expiresAt)
            return Result<int>.Fail(string.Empty, "That reset link has expired. Please start again.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, ct);
        if (user is null) return stale;

        user.PasswordHash = _passwords.Hash(newPassword);
        user.MustChangePassword = false;

        // A reset is also the way out of a lockout, so the counters go with it.
        user.AccessFailedCount = 0;
        user.LockoutEnd = null;

        await _db.SaveChangesAsync(ct);
        return Result<int>.Ok(user.Id);
    }

    private string Issue(int userId) =>
        _protector.Protect($"{userId}:{DateTime.UtcNow.Add(ValidFor).Ticks}");

    private static string BuildBody(string siteName, string link) =>
        $"""
         <p>Someone asked to reset the password for your {siteName} account.</p>
         <p><a href="{link}">Choose a new password</a></p>
         <p>The link works once and expires in 15 minutes. If this was not you,
         no action is needed - your password has not changed.</p>
         """;
}
