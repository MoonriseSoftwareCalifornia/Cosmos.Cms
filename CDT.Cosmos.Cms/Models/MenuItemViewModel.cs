using CDT.Cosmos.Cms.Common.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CDT.Cosmos.Cms.Models
{
    /// <summary>
    /// Menu item view model
    /// </summary>
    public class MenuItemViewModel
    {
        /// <summary>
        /// ID of menu item
        /// </summary>
        [Key]
        [ScaffoldColumn(false)]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        /// <summary>
        /// Menu item has children
        /// </summary>
        [ScaffoldColumn(false)]
        public bool hasChildren { get; set; }
        /// <summary>
        /// Parent ID of this item
        /// </summary>
        [ScaffoldColumn(false)]
        public int? ParentId { get; set; }
        /// <summary>
        /// Sort order indication
        /// </summary>
        [Display(Name = "Sort Order")]
        public int SortOrder { get; set; } = 0;
        /// <summary>
        /// Menu item text
        /// </summary>
        [Display(Name = "Menu Text")]
        public string MenuText { get; set; }
        /// <summary>
        /// URL path of item
        /// </summary>
        [UIHint("Url")]
        [Display(Name = "Url or Path")]
        public string Url { get; set; }
        /// <summary>
        /// CSS Icon class
        /// </summary>
        [Display(Name = "Icon")]
        [UIHint("Icon")]
        public string IconCode { get; set; }
        /// <summary>
        /// returns a new menu item
        /// </summary>
        /// <returns></returns>
        public MenuItem ToEntity()
        {
            return new()
            {
                Id = Id,
                HasChildren = hasChildren,
                ParentId = ParentId,
                SortOrder = SortOrder,
                MenuText = MenuText,
                Url = Url,
                IconCode = IconCode
            };
        }
    }
}