using System.ComponentModel.DataAnnotations;
using NEXUS.Services.Billing;

namespace NEXUS.Models.ViewModels;

/**
 * One-off charges: a replacement charge when a customer damages equipment,
 * or a manual adjustment accounts need to make. They sit pending until the
 * next bill for the connection sweeps them up.
 */
public sealed class ChargePage
{
    public IReadOnlyList<PendingCharge> Charges { get; set; } = Array.Empty<PendingCharge>();

    public IReadOnlyList<ChargeableConnection> Connections { get; set; } =
        Array.Empty<ChargeableConnection>();

    public RaiseChargeInput Input { get; set; } = new();

    public int? FilterConnectionId { get; set; }

    public bool IsEmpty => Charges.Count == 0;
}

public sealed class RaiseChargeInput
{
    [Range(1, int.MaxValue, ErrorMessage = "Pick a connection.")]
    [Display(Name = "Connection")]
    public int ConnectionId { get; set; }

    [Required(ErrorMessage = "Say what the charge is for.")]
    [MaxLength(200)]
    [Display(Name = "What it is for")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Charge type")]
    public string ChargeType { get; set; } = "Replacement";

    [Range(0.01, 1000000, ErrorMessage = "Enter an amount greater than zero.")]
    [Display(Name = "Amount")]
    public decimal Amount { get; set; }

    public static IReadOnlyList<string> TypeOptions { get; } = new[]
    {
        "Replacement",
        "Adjustment",
        "Equipment"
    };
}
