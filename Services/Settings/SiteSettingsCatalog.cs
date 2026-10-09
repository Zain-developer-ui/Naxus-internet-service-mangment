namespace NEXUS.Services.Settings;

/**
 * The settings the admin screen knows about. Keeping the catalogue here rather
 * than in the database means a fresh install renders a full form on first load,
 * and a key removed from this list stops being editable without needing a data
 * migration.
 */
public sealed record SiteSettingDefinition(
    string Key,
    string Label,
    string Group,
    string Kind,
    string Default,
    string? Hint = null);

public static class SiteSettingsCatalog
{
    public const string General = "General";
    public const string Branding = "Branding";
    public const string Features = "Features";
    public const string Contact = "Contact";
    public const string Security = "Security";

    public static readonly IReadOnlyList<SiteSettingDefinition> All = new[]
    {
        new SiteSettingDefinition("site.name", "Site name", General, "text", "NEXUS",
            "Shown in the navbar and page titles."),
        new SiteSettingDefinition("site.tagline", "Tagline", General, "text",
            "Connecting You to a Better Tomorrow", "Appears under the logo on the home page."),
        new SiteSettingDefinition("site.company", "Registered company", General, "text",
            "NEXUS Service Marketing System"),
        new SiteSettingDefinition("site.footer", "Footer note", General, "text",
            "All rights reserved."),

        new SiteSettingDefinition("brand.primary", "Primary colour", Branding, "text", "#0A1F44",
            "Hex value used for headers and primary buttons."),
        new SiteSettingDefinition("brand.accent", "Accent colour", Branding, "text", "#1E6FE5"),
        new SiteSettingDefinition("brand.logo", "Logo path", Branding, "text", "/img/logo.svg",
            "Relative path under wwwroot."),

        new SiteSettingDefinition("feature.registration", "Open public registration",
            Features, "bool", "true",
            "Off means the sign-up form refuses new connections; staff can still register walk-ins."),
        new SiteSettingDefinition("feature.maintenance", "Maintenance mode", Features, "bool", "false",
            "Shows a notice site-wide. Staff dashboards stay reachable."),
        new SiteSettingDefinition("feature.feedback", "Collect customer feedback", Features, "bool", "true"),
        new SiteSettingDefinition("feature.autoInvoice", "Raise bills automatically",
            Features, "bool", "true", "Monthly billing run; off means bills are raised by hand."),

        new SiteSettingDefinition("contact.phone", "Support phone", Contact, "text", "+92 42 111 000 000"),
        new SiteSettingDefinition("contact.email", "Support email", Contact, "text", "support@nexus.example"),
        new SiteSettingDefinition("contact.address", "Head office", Contact, "text", "Lahore, Pakistan"),

        new SiteSettingDefinition("security.sessionMinutes", "Session timeout (minutes)",
            Security, "int", "120", "Idle minutes before a signed-in user is returned to the login page."),
        new SiteSettingDefinition("security.maxAttempts", "Lockout after failed logins",
            Security, "int", "5")
    };

    /** Group order as rendered on the settings screen. */
    public static readonly IReadOnlyList<string> Groups = new[]
        { General, Branding, Features, Contact, Security };
}
