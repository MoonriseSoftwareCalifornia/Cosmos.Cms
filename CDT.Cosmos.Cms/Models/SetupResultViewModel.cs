using System;
using System.ComponentModel.DataAnnotations;

namespace CDT.Cosmos.Cms.Models
{
    /// <summary>
    /// Setup view model
    /// </summary>
    public class SetupViewModel
    {
        /// <summary>
        /// Icons are setup.
        /// </summary>
        [Obsolete("Icon classes are no longer part of the install.", true)]
        [Display(Name = "Icons")]
        public bool IconsSetup { get; set; } = false;
        /// <summary>
        /// Roles are setup.
        /// </summary>
        [Display(Name = "User Roles")] public bool RolesSetup { get; set; } = false;
        /// <summary>
        /// Administrator account setup.
        /// </summary>
        [Display(Name = "Administrator Setup")]
        public bool SetupAdmin { get; set; } = false;
        /// <summary>
        /// Redis cache detected.
        /// </summary>
        [Display(Name = "REDIS Cache Detected")]
        [Obsolete("Redis no longer used.", true)]
        public bool RedisSetup { get; set; } = false;
        /// <summary>
        /// Email account setup.
        /// </summary>
        [Display(Name = "Email")]
        public bool EmailSetup { get; set; } = false;
        /// <summary>
        /// Database is setup.
        /// </summary>
        [Display(Name = "Database")]
        public bool DataSetup { get; set; } = false;
    }
}