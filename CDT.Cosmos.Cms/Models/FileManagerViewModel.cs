using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace CDT.Cosmos.Cms.Models
{
    /// <summary>
    /// File manager view model
    /// </summary>
    public class FileManagerViewModel
    {
        /// <summary>
        /// Team ID
        /// </summary>
        public int? TeamId { get; set; }
        /// <summary>
        /// Team folders
        /// </summary>
        public IEnumerable<SelectListItem> TeamFolders { get; set; }
    }
}