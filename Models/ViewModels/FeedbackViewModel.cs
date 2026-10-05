using System.ComponentModel.DataAnnotations;

namespace NEXUS.Models.ViewModels
{
    public class FeedbackViewModel
    {
        [Required(ErrorMessage = "Please select an overall rating")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int OverallRating { get; set; }

        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int ServiceRating { get; set; }

        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int InstallationRating { get; set; }

        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int SupportRating { get; set; }

        [Required(ErrorMessage = "Please choose a category")]
        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your feedback")]
        [StringLength(1000, MinimumLength = 5, ErrorMessage = "Feedback must be 5–1000 characters")]
        public string Message { get; set; } = string.Empty;
    }
}