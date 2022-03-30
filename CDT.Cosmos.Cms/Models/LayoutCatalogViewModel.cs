using System.ComponentModel.DataAnnotations;

namespace CDT.Cosmos.Cms.Models
{
    /// <summary>
    /// Layout catalog view model
    /// </summary>
    public class LayoutCatalogViewModel
    {
        /// <summary>
        /// Layout ID
        /// </summary>
        [Key]
        public string Id { get; set; }
        /// <summary>
        /// Layout name
        /// </summary>
        [Display(Name = "Layout Name")]
        public string Name { get; set; }
        /// <summary>
        /// Layout description
        /// </summary>
        [Display(Name = "Description/Notes")]
        public string Description { get; set; }
        /// <summary>
        /// Layout license
        /// </summary>
        [Display(Name = "License")]
        public string License { get; set; }
    }
}
