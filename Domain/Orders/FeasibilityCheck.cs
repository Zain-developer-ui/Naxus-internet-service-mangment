using System.ComponentModel.DataAnnotations;
using NEXUS.Common.Constants;
using NEXUS.Domain.Organisation;

namespace NEXUS.Domain.Orders;

/**
 * The SRS requires a survey before a line is committed: distance from the
 * exchange and server capacity decide whether service is even possible.
 * A Dial-Up order additionally proves the landline exists.
 */
public class FeasibilityCheck : AuditableEntity
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public ConnectionOrder Order { get; set; } = null!;

    public FeasibilityResult Result { get; set; } = FeasibilityResult.Pending;

    public double? DistanceFromExchangeKm { get; set; }

    public bool ExchangeHasCapacity { get; set; }

    public bool LandlineVerified { get; set; }

    [MaxLength(500)]
    public string Remarks { get; set; } = string.Empty;

    public int? CheckedById { get; set; }
    public Employee? CheckedBy { get; set; }

    public DateTime? CheckedAt { get; set; }

    public bool IsResolved => Result != FeasibilityResult.Pending;

    public string ResultLabel => Result switch
    {
        FeasibilityResult.Passed => "Feasible",
        FeasibilityResult.Failed => "Not Feasible",
        _ => "Pending"
    };
}
