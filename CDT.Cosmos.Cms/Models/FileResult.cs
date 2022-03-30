namespace CDT.Cosmos.Cms.Models
{
    /// <summary>
    /// File upload result
    /// </summary>
    public class FileUploadResult
    {
        /// <summary>
        /// File is uploaded
        /// </summary>
        public bool uploaded { get; set; }
        /// <summary>
        /// File upload unique ID
        /// </summary>
        public string fileUid { get; set; }
    }
}