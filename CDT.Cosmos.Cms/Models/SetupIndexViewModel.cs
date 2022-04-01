using System.ComponentModel.DataAnnotations;

namespace CDT.Cosmos.Cms.Models
{
    /// <summary>
    /// Setup controller index view model.
    /// </summary>
    public class SetupIndexViewModel
    {
        /// <summary>
        /// Setup state we are in.
        /// </summary>
        public SetupState SetupState { get; set; }
        /// <summary>
        /// Administrator email address
        /// </summary>
        [Display(Name = "Administrator Email Address")]
        [EmailAddress]
        [DataType(DataType.EmailAddress)]
        public string AdminEmail { get; set; }
        /// <summary>
        /// Password
        /// </summary>
        [Display(Name = "Password")]
        [DataType(DataType.Password)]
        [Required(AllowEmptyStrings = false)]
        public string Password { get; set; }
        /// <summary>
        /// Password confirmation
        /// </summary>
        [Display(Name = "Confirm Password")]
        [DataType(DataType.Password)]
        [Compare("Password")]
        public string ConfirmPassword { get; set; }

    }

    /// <summary>
    /// Set
    /// </summary>
    public enum SetupState
    {
        /// <summary>
        /// Setup administrator account.
        /// </summary>
        SetupAdmin,
        /// <summary>
        /// Database is installed but needed to apply missing migrations.
        /// </summary>
        Upgrade,
        /// <summary>
        /// Database is up to date.
        /// </summary>
        UpToDate
    }
}
