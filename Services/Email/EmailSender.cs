using System.Net;
using System.Net.Mail;
using NEXUS.Common;
using NEXUS.Services.Settings;

namespace NEXUS.Services.Email;

/**
 * Outgoing mail. Every value it needs comes off the settings screen, so an
 * administrator can point the site at a real mailbox without a redeploy.
 *
 * With the switch off, or the host or sender missing, the settings are not
 * usable and SendAsync says so - the caller is expected to fall back to whatever
 * it can do without a mail server rather than fail the user's request.
 */
public sealed record EmailSettings(string Host, int Port, string User, string Password,
                                   string From, bool UseSsl, bool Enabled)
{
    public bool IsUsable =>
        Enabled && !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(From);
}

public interface IEmailSender
{
    Task<EmailSettings> CurrentAsync(CancellationToken ct = default);

    Task<Result<bool>> SendAsync(string to, string subject, string body,
                                 CancellationToken ct = default);
}

public sealed class EmailSender : IEmailSender
{
    private readonly ISiteSettingsService _settings;

    public EmailSender(ISiteSettingsService settings) => _settings = settings;

    public async Task<EmailSettings> CurrentAsync(CancellationToken ct = default)
    {
        var values = await _settings.GetManyAsync(new[]
        {
            "smtp.enabled", "smtp.host", "smtp.port", "smtp.user",
            "smtp.password", "smtp.from", "smtp.ssl"
        }, ct);

        string Read(string key, string fallback) =>
            values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value
                : fallback;

        var user = Read("smtp.user", string.Empty);

        return new EmailSettings(
            Read("smtp.host", string.Empty),
            int.TryParse(Read("smtp.port", "587"), out var port) ? port : 587,
            user,
            Read("smtp.password", string.Empty),
            Read("smtp.from", user),
            Read("smtp.ssl", "true") == "true",
            Read("smtp.enabled", "false") == "true");
    }

    public async Task<Result<bool>> SendAsync(string to, string subject, string body,
                                              CancellationToken ct = default)
    {
        var config = await CurrentAsync(ct);

        if (!config.IsUsable)
            return Result<bool>.Fail(ErrorKind.Validation, "Email is not configured.");

        try
        {
            using var message = new MailMessage(config.From, to, subject, body)
            {
                IsBodyHtml = true
            };

            using var client = new SmtpClient(config.Host, config.Port)
            {
                EnableSsl = config.UseSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false
            };

            if (!string.IsNullOrWhiteSpace(config.User))
                client.Credentials = new NetworkCredential(config.User, config.Password);

            await client.SendMailAsync(message, ct);
            return Result<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ErrorKind.Failure,
                $"The mail server refused the message: {ex.Message}");
        }
    }
}
