using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CDT.Cosmos.Cms.Models
{
    /// <summary>
    /// Status Index View Model
    /// </summary>
    public class StatusIndexViewModel
    {
        /// <summary>
        /// Resource statuses
        /// </summary>
        public List<StatusIndexItem> Items { get; set; }
    }

    /// <summary>
    /// Resouce Item Status
    /// </summary>
    public class StatusIndexItem
    {
        /// <summary>
        /// Resource type.
        /// </summary>
        [Display(Name = "Resource Type")]
        public ResourceType ResourceType { get; set; }

        /// <summary>
        /// Identifier of the resource.
        /// </summary>
        [Display(Name = "Identifier")]
        public string Identifier { get; set; }

        /// <summary>
        /// Resource is operational.
        /// </summary>
        [Display(Name = "Status")]
        public bool IsValid { get; set; }

        /// <summary>
        /// Any messages, informational, error or otherwise.
        /// </summary>
        [Display(Name = "Messages")]
        public string Messages { get; set; }
    }

    /// <summary>
    /// Cloud Resource Type
    /// </summary>
    public enum ResourceType
    {
        /// <summary>
        /// SQL Database
        /// </summary>
        SqlDatabase = 0,
        /// <summary>
        /// BLOB Storage
        /// </summary>
        BlobStorage = 1,
        /// <summary>
        /// Redis Cache
        /// </summary>
        RedisCache = 2,
        /// <summary>
        /// Content Distribution Network
        /// </summary>
        Cdn = 3,
        /// <summary>
        /// Cosmos editor
        /// </summary>
        CosmosEditor = 4
    }

}
