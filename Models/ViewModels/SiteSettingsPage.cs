namespace NEXUS.Models.ViewModels;

public sealed record SiteSettingField(
    string Key,
    string Label,
    string Kind,
    string Value,
    string? Hint);

public sealed record SiteSettingGroup(string Name, IReadOnlyList<SiteSettingField> Fields);

/**
 * The website settings screen. Groups come from SiteSettingsCatalog so the
 * form renders whatever the catalogue declares, with the stored value layered
 * over the default.
 */
public sealed class SiteSettingsPage
{
    public List<SiteSettingGroup> Groups { get; } = new();
    public DateTime? UpdatedAt { get; set; }
    public SystemRuntime? Runtime { get; set; }
}
