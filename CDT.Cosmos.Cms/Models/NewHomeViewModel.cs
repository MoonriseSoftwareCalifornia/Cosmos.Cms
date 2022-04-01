using System.ComponentModel.DataAnnotations;

namespace CDT.Cosmos.Cms.Models
{
    /// <summary>
    /// New home page view model
    /// </summary>
    public class NewHomeViewModel
    {
        /// <summary>
        /// Article ID
        /// </summary>
        [Key]
        [Display(Name = "Article Key")]
        public int Id { get; set; }
        /// <summary>
        /// Article number
        /// </summary>
        [Display(Name = "Article Number")]
        public int ArticleNumber { get; set; }
        /// <summary>
        /// Article title
        /// </summary>
        [Display(Name = "Page Title")]
        public string Title { get; set; }
        /// <summary>
        /// Indicates to make this the new home page
        /// </summary>
        [Display(Name = "Make this the new home page")]
        public bool IsNewHomePage { get; set; }
        /// <summary>
        /// Article URL path
        /// </summary>
        [Display(Name = "URL Path")]
        public string UrlPath { get; set; }
    }
}