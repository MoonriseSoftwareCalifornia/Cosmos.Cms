using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace CDT.Cosmos.Cms.Models
{
    /// <summary>
    /// Layout file upload view model
    /// </summary>
    public class LayoutFileUploadViewModel
    {
        /// <summary>
        /// Layout ID number (once saved)
        /// </summary>
        [Display(Name = "Choose layout to replace:")]
        public int? Id { get; set; }

        /// <summary>
        /// Layout name
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        [Display(Name = "Layout name:")]
        public string Name { get; set; }
        /// <summary>
        /// Layout description
        /// </summary>
        [Display(Name = "Description/Notes:")]
        public string Description { get; set; }

        /// <summary>
        /// Layer file to upload
        /// </summary>
        [Display(Name = "Select file to upload:")]
        public IFormFile File { get; set; }

    }
}
