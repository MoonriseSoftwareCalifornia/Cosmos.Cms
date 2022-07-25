using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CDT.Cosmos.Cms.Common.Data
{
    /// <summary>
    ///     A team member
    /// </summary>
    public class TeamMember
    {
        /// <summary>
        ///     Identity key for this entity
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        ///     The role ID of this team member as defined by <see cref="CDT.Cosmos.Cms.Common.Models.TeamRoleEnum" />
        /// </summary>
        public int TeamRole { get; set; }

        /// <summary>
        ///     ID of the team this member belongs to
        /// </summary>
        public int TeamId { get; set; }

        /// <summary>
        ///     The team
        /// </summary>
        [ForeignKey("TeamId")]
        public Team Team { get; set; }

        /// <summary>
        ///     The identity user associated with this membership
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        ///     The identity user associated with this membership
        /// </summary>
        [ForeignKey("UserId")]
        public IdentityUser User { get; set; }
    }
}