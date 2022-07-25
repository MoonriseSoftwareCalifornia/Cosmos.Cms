using System;
using System.ComponentModel.DataAnnotations;

namespace CDT.Cosmos.Cms.Models
{
    /// <summary>
    /// Configuration list item
    /// </summary>
    public class ConfigListItemViewModel
    {
        /// <summary>
        /// Configuration ID
        /// </summary>
        [Key]
        [Display(Name = "ID")]
        public int Id { get; set; }

        /// <summary>
        /// Created date and time
        /// </summary>
        [Required]
        [Display(Name = "Created")]
        public DateTimeOffset Created { get; set; }

        /// <summary>
        /// Updated date and time
        /// </summary>
        [Required]
        [Display(Name = "Updated")]
        public DateTimeOffset Updated { get; set; }

        /// <summary>
        /// User Id of person who created configuration
        /// </summary>
        [Required]
        [Display(Name = "User Id")]
        public string UserId { get; set; }

        /// <summary>
        /// Configuration is active
        /// </summary>
        [Required]
        [Display(Name = "Is Active")]
        public bool IsActive { get; set; }

        /// <summary>
        /// The identity user associated with this configuration
        /// </summary>
        [Display(Name = "Updated by")]
        public string UserEmail { get; set; }

        /// <summary>
        /// Notes
        /// </summary>
        [Display(Name = "Notes")]
        [DataType(DataType.Html)]
        public string Notes { get; set; }
    }
}
