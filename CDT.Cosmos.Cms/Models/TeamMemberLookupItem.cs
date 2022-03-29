using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace CDT.Cosmos.Cms.Models
{
    /// <summary>
    /// Team member lookup item
    /// </summary>
    public class TeamMemberLookupItem
    {
        /// <summary>
        /// Empty constructor
        /// </summary>
        public TeamMemberLookupItem()
        {
        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="user"></param>
        public TeamMemberLookupItem(IdentityUser user)
        {
            UserEmail = user.Email;
            UserId = user.Id;
        }
        /// <summary>
        /// Team member user ID
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Team member Email address
        /// </summary>
        [Display(Name = "User Email")]
        [EmailAddress]
        public string UserEmail { get; set; }
    }
}