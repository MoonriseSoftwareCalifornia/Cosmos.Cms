namespace CDT.Cosmos.Cms.Models
{
    /// <summary>
    /// Database installation view model
    /// </summary>
    public class InstallDatabaseViewModel
    {
        /// <summary>
        /// Indicates a successful install
        /// </summary>
        public bool Success { get; set; }
        /// <summary>
        /// Data source
        /// </summary>
        public string DataSource { get; set; }
        /// <summary>
        /// Install message(s)
        /// </summary>
        public string Message { get; set; }
    }
}