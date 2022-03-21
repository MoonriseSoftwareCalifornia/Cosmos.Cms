using System.ComponentModel.DataAnnotations;

namespace CDT.Cosmos.Cms.Models
{
    /// <summary>
    /// Template index view model
    /// </summary>
    public class TemplateIndexViewModel
    {
        /// <summary>
        /// Unique ID
        /// </summary>
        [Key] public int Id { get; set; }
        /// <summary>
        /// Name of the associated layout
        /// </summary>
        [Display(Name = "Associated Layout")]
        public string LayoutName { get; set; }
        /// <summary>
        /// Template title
        /// </summary>
        [Display(Name = "Template Title")]
        [StringLength(128)]
        public string Title { get; set; }
        /// <summary>
        /// Description/Notes regarding this template
        /// </summary>
        [Display(Name = "Description/Notes")] public string Description { get; set; }
    }
}