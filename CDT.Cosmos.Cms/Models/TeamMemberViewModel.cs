using CDT.Cosmos.Cms.Common.Models;
using System.ComponentModel.DataAnnotations;

namespace CDT.Cosmos.Cms.Models
{
    /// <summary>
    /// Team member view model
    /// </summary>
    public class TeamMemberViewModel
    {
        /// <summary>
        /// ID of the team member
        /// </summary>
        [Key]
        [Display(Name = "Team Member Id")]
        public int Id { get; set; }

        /// <summary>
        ///     The role ID of this team member as defined by <see cref="TeamRoleEnum" />
        /// </summary>
        [Display(Name = "Team Role")]
        [UIHint("TeamMemberRole")]
        public TeamRoleLookupItem TeamRole { get; set; } = new(TeamRoleEnum.Reviewer);
        /// <summary>
        /// Team view model
        /// </summary>
        public TeamViewModel Team { get; set; }
        /// <summary>
        /// Team member lookup item
        /// </summary>
        [Display(Name = "Member")]
        [UIHint("TeamMember")]
        public TeamMemberLookupItem Member { get; set; }
    }
}