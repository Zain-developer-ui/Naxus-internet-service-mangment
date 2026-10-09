using Microsoft.EntityFrameworkCore;
using NEXUS.Common;
using NEXUS.Data;
using NEXUS.Domain.Organisation;
using NEXUS.Models.ViewModels;

namespace NEXUS.Services.Settings;

/**
 * Reads and writes the website settings. The catalogue in SiteSettingsCatalog
 * decides which keys exist; this service only stores values, so a key that is
 * missing from the table simply falls back to its catalogue default.
 */
public interface ISiteSettingsService
{
    Task<SiteSettingsPage> GetAsync(CancellationToken ct = default);
    Task<Result<bool>> SaveAsync(IReadOnlyDictionary<string, string?> values,
                                 CancellationToken ct = default);

    /** Resolved value for a key, or its default. Used by the public layout. */
    Task<string> GetValueAsync(string key, CancellationToken ct = default);

    Task<bool> GetFlagAsync(string key, CancellationToken ct = default);
}

public sealed class SiteSettingsService : ISiteSettingsService
{
    private readonly NexusDbContext _db;

    public SiteSettingsService(NexusDbContext db) => _db = db;

    public async Task<SiteSettingsPage> GetAsync(CancellationToken ct = default)
    {
        var stored = await _db.SiteSettings.AsNoTracking()
            .ToDictionaryAsync(s => s.Key, s => s.Value, StringComparer.OrdinalIgnoreCase, ct);

        var page = new SiteSettingsPage();

        foreach (var group in SiteSettingsCatalog.Groups)
        {
            var fields = SiteSettingsCatalog.All
                .Where(d => d.Group == group)
                .Select(d =>
                {
                    var value = stored.TryGetValue(d.Key, out var v) && !string.IsNullOrEmpty(v)
                        ? v
                        : d.Default;

                    return new SiteSettingField(d.Key, d.Label, d.Kind, value, d.Hint);
                })
                .ToList();

            page.Groups.Add(new SiteSettingGroup(group, fields));
        }

        page.UpdatedAt = await _db.SiteSettings.AsNoTracking()
            .Where(s => s.UpdatedAt != null)
            .OrderByDescending(s => s.UpdatedAt)
            .Select(s => s.UpdatedAt)
            .FirstOrDefaultAsync(ct);

        return page;
    }

    public async Task<Result<bool>> SaveAsync(IReadOnlyDictionary<string, string?> values,
                                              CancellationToken ct = default)
    {
        if (values.Count == 0)
            return Result<bool>.Fail(ErrorKind.Validation, "Nothing to save.");

        // Reject a value that does not parse for its declared kind before any
        // row is touched, so a bad input cannot leave half the form written.
        var errors = new Dictionary<string, string[]>();

        foreach (var definition in SiteSettingsCatalog.All)
        {
            if (!values.TryGetValue(definition.Key, out var raw)) continue;

            var probe = definition.Kind switch
            {
                "int" => int.TryParse(raw, out var parsed) && parsed >= 0 ? null : "Enter a whole number of 0 or more.",
                "bool" => NormalizeFlag(raw) is not null ? null : "Choose on or off.",
                _ => null
            };

            if (probe is not null) errors[definition.Key] = new[] { probe };
        }

        if (errors.Count > 0)
            return Result<bool>.Invalid(errors);

        var existing = await _db.SiteSettings
            .Where(s => values.Keys.Contains(s.Key))
            .ToListAsync(ct);

        var byKey = existing.ToDictionary(s => s.Key, StringComparer.OrdinalIgnoreCase);
        var now = DateTime.UtcNow;

        foreach (var definition in SiteSettingsCatalog.All)
        {
            if (!values.TryGetValue(definition.Key, out var raw)) continue;

            var normalised = definition.Kind switch
            {
                "bool" => NormalizeFlag(raw) ?? "false",
                "int" => (int.TryParse(raw, out var n) ? n : 0).ToString(),
                _ => (raw ?? string.Empty).Trim()
            };

            if (byKey.TryGetValue(definition.Key, out var row))
            {
                if (row.Value == normalised) continue;
                row.Value = normalised;
                row.UpdatedAt = now;
            }
            else
            {
                _db.SiteSettings.Add(new SiteSetting
                {
                    Key = definition.Key,
                    Value = normalised,
                    Kind = definition.Kind,
                    CreatedAt = now
                });
            }
        }

        await _db.SaveChangesAsync(ct);
        return Result<bool>.Ok(true);
    }

    public async Task<string> GetValueAsync(string key, CancellationToken ct = default)
    {
        var stored = await _db.SiteSettings.AsNoTracking()
            .Where(s => s.Key == key)
            .Select(s => s.Value)
            .FirstOrDefaultAsync(ct);

        if (!string.IsNullOrEmpty(stored)) return stored;

        return SiteSettingsCatalog.All.FirstOrDefault(d => d.Key == key)?.Default ?? string.Empty;
    }

    public async Task<bool> GetFlagAsync(string key, CancellationToken ct = default)
        => NormalizeFlag(await GetValueAsync(key, ct)) == "true";

    /** A checkbox only posts when ticked, so absence has to mean "false". */
    private static string? NormalizeFlag(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "false";

        return raw.Trim().ToLowerInvariant() switch
        {
            "true" or "on" or "1" or "yes" => "true",
            "false" or "off" or "0" or "no" => "false",
            _ => null
        };
    }
}
