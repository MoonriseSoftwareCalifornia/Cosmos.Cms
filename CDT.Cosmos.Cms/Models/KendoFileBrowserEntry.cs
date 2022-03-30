using Kendo.Mvc.UI;

namespace CDT.Cosmos.Cms.Models
{
    /// <summary>
    /// Kendo file browser entry
    /// </summary>
    public class KendoFileBrowserEntry
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public KendoFileBrowserEntry()
        {
        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="entry"></param>
        public KendoFileBrowserEntry(FileBrowserEntry entry)
        {
            name = entry.Name;
            type = entry.EntryType == FileBrowserEntryType.Directory ? "d" : "f";
            size = entry.Size.ToString();
        }

        /// <summary>
        ///     File Name
        /// </summary>
        public string name { get; set; }

        /// <summary>
        ///     f (file) or d (directory)
        /// </summary>
        public string type { get; set; }

        /// <summary>
        ///     Size in bytes
        /// </summary>
        public string size { get; set; }
    }
}