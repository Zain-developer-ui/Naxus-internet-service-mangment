using System.ComponentModel.DataAnnotations;

namespace NEXUS.Domain.Organisation;

/**
 * Website settings are a key/value store rather than a wide table. New
 * switches get added every few weeks and a migration per toggle is not worth
 * it; the read path caches the whole set anyway, so the extra join costs
 * nothing at request time.
 */
public class SiteSetting : AuditableEntity
{
    public int Id { get; set; }

    [Required, MaxLength(80)]
    public string Key { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Value { get; set; } = string.Empty;

    /** "text", "bool", "int" - tells the admin screen which control to render. */
    [Required, MaxLength(16)]
    public string Kind { get; set; } = "text";
}
