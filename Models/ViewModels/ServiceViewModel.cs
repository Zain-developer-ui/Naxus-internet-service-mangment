namespace NEXUS.Models.ViewModels
{
    /// <summary>
    /// A single service offering (Broadband, Fiber, Wireless, etc.)
    /// used on the Home preview and the full Services page.
    /// </summary>
    public class ServiceViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string ShortDescription { get; set; } = string.Empty;
        public string FullDescription { get; set; } = string.Empty;
        public string IconClass { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string SpeedRange { get; set; } = string.Empty;
        public string Availability { get; set; } = string.Empty;
        public List<string> Features { get; set; } = new();
        public List<string> Benefits { get; set; } = new();
    }
}