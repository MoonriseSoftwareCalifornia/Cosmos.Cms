using System.ComponentModel.DataAnnotations;

namespace CDT.Cosmos.Cms.Models
{
    /// <summary>
    /// Team view model
    /// </summary>
    public class TeamViewModel
    {
        /// <summary>
        /// Team ID
        /// </summary>
        [Key] [Display(Name = "Team ID")] public int Id { get; set; }
        /// <summary>
        /// Name of team
        /// </summary>
        [MaxLength(64)]
        [Display(Name = "Team Name")]
        public string TeamName { get; set; }
        /// <summary>
        /// Description of team
        /// </summary>
        [MaxLength(1024)]
        [DataType(DataType.Html)]
        [Display(Name = "Team Description")]
        public string TeamDescription { get; set; }
    }
}