namespace NEXUS.Models.ViewModels
{
    /// <summary>
    /// A single service offering (Broadband, Fiber, Wireless, etc.)
    /// used on the Home preview and the full Services page.
    /// </summary>
    public class ServiceViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string ShortDescription { get; set; }
        public string FullDescription { get; set; }
        public string IconClass { get; set; }
        public string ImageUrl { get; set; }
        public string SpeedRange { get; set; }
        public string Availability { get; set; }
        public List<string> Features { get; set; }
        public List<string> Benefits { get; set; }
    }
}