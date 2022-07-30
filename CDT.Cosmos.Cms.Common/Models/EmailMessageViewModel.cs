using System.ComponentModel.DataAnnotations;

namespace CDT.Cosmos.Cms.Common.Models
{
    /// <summary>
    /// Email message view model
    /// </summary>
    public class EmailMessageViewModel
    {
        /// <summary>
        /// Sender name
        /// </summary>
        [Display(Name = "Your name:")]
        public string SenderName { get; set; }
        /// <summary>
        /// Email address
        /// </summary>
        [EmailAddress]
        [MaxLength(100)]
        [Required]
        [Display( Name = "Your email address (required/will not be shared):")]
        public string FromEmail { get; set; }

        /// <summary>
        /// Email subject
        /// </summary>
        [MaxLength(128)]
        [Display(Name = "Subject (optional):")]
        public string Subject { get; set; }

        /// <summary>
        /// Email content
        /// </summary>
        [MinLength(5, ErrorMessage = "Minimum required 5 characters.")]
        [MaxLength(2048)]
        public string Content { get; set; }

        public bool? SendSuccess { get; set; }
    }
}
