namespace NEXUS.Models.ViewModels
{
    /// <summary>
    /// Internet plan (Basic / Standard / Premium). Used across
    /// Home preview, Plans page, Plan Details, and order forms.
    /// </summary>
    public class PlanViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;         // BASIC, STANDARD, PREMIUM
        public string ServiceType { get; set; } = "Broadband";   // Dial-Up or Broadband
        public int SpeedMbps { get; set; }
        public decimal MonthlyRental { get; set; }
        public decimal InstallationFee { get; set; } = 0m;
        public decimal SecurityDeposit { get; set; } = 450m;
        public bool IsPopular { get; set; }
        public string Tagline { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string RouterImageUrl { get; set; } = string.Empty;
        public List<string> Features { get; set; } = new();
        public List<string> Benefits { get; set; } = new();
    }
}