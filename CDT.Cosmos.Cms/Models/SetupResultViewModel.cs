using System.ComponentModel.DataAnnotations;

namespace CDT.Cosmos.Cms.Models
{
    public class SetupViewModel
    {
        /// <summary>
        /// Icons are setup
        /// </summary>
        [Display(Name = "Icons")] public bool IconsSetup { get; set; } = false;
        /// <summary>
        /// Roles are setup
        /// </summary>
        [Display(Name = "User Roles")] public bool RolesSetup { get; set; } = false;
        /// <summary>
        /// Administrator is setup
        /// </summary>
        [Display(Name = "Administrator Setup")]
        public bool SetupAdmin { get; set; } = false;
        /// <summary>
        /// Redis is setup
        /// </summary>
        [Display(Name = "REDIS Cache Detected")]
        public bool RedisSetup { get; set; } = false;
        /// <summary>
        /// Email is setup
        /// </summary>
        [Display(Name = "Email")] public bool EmailSetup { get; set; } = false;
        /// <summary>
        /// Database is setup
        /// </summary>
        [Display(Name = "Database")] public bool DataSetup { get; set; } = false;
    }
}