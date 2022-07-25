using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CDT.Cosmos.Cms.Common.Data
{
    /// <summary>
    /// Cosmos configuration
    /// </summary>
    public class Config
    {
        /// <summary>
        /// Configuration ID
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Created date and time
        /// </summary>
        [Required]
        public DateTimeOffset Created { get; set; }

        /// <summary>
        /// Updated date and time
        /// </summary>
        [Required]
        public DateTimeOffset Updated { get; set; }

        /// <summary>
        /// User Id of person who created configuration
        /// </summary>
        [Required]
        public string UserId { get; set; }

        /// <summary>
        /// Configuration is active
        /// </summary>
        [Required]
        public bool IsActive { get; set; }

        /// <summary>
        /// Configuration value
        /// </summary>
        [Required]
        public string Value { get; set; }

        /// <summary>
        /// The identity user associated with this configuration
        /// </summary>
        [ForeignKey("UserId")]
        public IdentityUser User { get; set; }

        /// <summary>
        /// Notes
        /// </summary>
        [DataType(DataType.Html)]
        public string Notes { get; set; }
    }
}
