using CDT.Cosmos.Cms.Models.Interfaces;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CDT.Cosmos.Cms.Models
{
    /// <summary>
    /// File manager edit code view model
    /// </summary>
    public class FileManagerEditCodeViewModel : ICodeEditorViewModel
    {
        /// <summary>
        /// Paths
        /// </summary>
        public string Path { get; set; }
        /// <summary>
        /// Content
        /// </summary>
        [DataType(DataType.Html)] public string Content { get; set; }
        /// <summary>
        /// Editor mode
        /// </summary>
        public EditorMode EditorMode { get; set; }
        /// <summary>
        /// File ID
        /// </summary>
        [Key]
        public int Id { get; set; }
        /// <summary>
        /// Editing field
        /// </summary>
        public string EditingField { get; set; }
        /// <summary>
        /// Editor title
        /// </summary>
        public string EditorTitle { get; set; }
        /// <summary>
        /// Editing fields
        /// </summary>
        public IEnumerable<EditorField> EditorFields { get; set; }
        /// <summary>
        /// Custom buttons
        /// </summary>
        public IEnumerable<string> CustomButtons { get; set; }
        /// <summary>
        /// Code is valid
        /// </summary>
        public bool IsValid { get; set; }
    }
}