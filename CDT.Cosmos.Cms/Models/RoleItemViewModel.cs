using System;
using System.ComponentModel.DataAnnotations;

namespace CDT.Cosmos.Cms.Models
{
    /// <summary>
    /// Role item view model
    /// </summary>
    [Serializable]
    public class RoleItemViewModel
    {
        /// <summary>
        ///     Role ID
        /// </summary>
        [Key]
        [Display(Name = "Role ID")]
        public string Id { get; set; }

        /// <summary>
        ///     Friendly role name
        /// </summary>
        [Display(Name = "Role Name")]
        public string RoleName { get; set; }

        /// <summary>
        ///     Role used to search on
        /// </summary>
        [Display(Name = "Role Normalized Name")]
        public string RoleNormalizedName { get; set; }
    }
}