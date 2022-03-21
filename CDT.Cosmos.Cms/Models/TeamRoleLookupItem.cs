using CDT.Cosmos.Cms.Common.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace CDT.Cosmos.Cms.Models
{
    /// <summary>
    /// Team role lookup item
    /// </summary>
    public class TeamRoleLookupItem
    {
        /// <summary>
        /// Empty constructor
        /// </summary>
        public TeamRoleLookupItem()
        {
        }
        /// <summary>
        /// Lookup team role
        /// </summary>
        /// <param name="role"></param>
        public TeamRoleLookupItem(TeamRoleEnum role)
        {
            TeamRoleId = (int)role;
            TeamRoleName = Enum.GetName(typeof(TeamRoleEnum), role);
        }
        /// <summary>
        /// ID of Team Role
        /// </summary>
        [Key] 
        public int TeamRoleId { get; set; }
        /// <summary>
        /// Name of team role
        /// </summary>
        public string TeamRoleName { get; set; }
    }
}