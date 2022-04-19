namespace CDT.Cosmos.Cms.Models
{

    /// <summary>
    /// Page import result
    /// </summary>
    public class PageImportResult : FileUploadResult
    {
        /// <summary>
        /// Errors
        /// </summary>
        public string Errors { get; set; } = string.Empty;
    }
}