using CDT.Cosmos.Cms.Common.Data;
using CDT.Cosmos.Cms.Common.Data.Logic;
using System;
using System.ComponentModel.DataAnnotations;

namespace CDT.Cosmos.Cms.Models
{
    /// <summary>
    /// Team article view model
    /// </summary>
    public class TeamArticleViewModel
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public TeamArticleViewModel()
        {
        }
        /// <summary>
        /// Alternate constructor
        /// </summary>
        /// <param name="article"></param>
        public TeamArticleViewModel(Article article)
        {
            Id = article.Id;
            StatusCode = (StatusCodeEnum)article.StatusCode;
        }
        /// <summary>
        /// Article ID
        /// </summary>
        [Key] public int Id { get; set; }
        /// <summary>
        /// Status code
        /// </summary>
        public StatusCodeEnum StatusCode { get; set; }
        /// <summary>
        /// Article number
        /// </summary>
        public int ArticleNumber { get; set; }
        /// <summary>
        /// Url path
        /// </summary>
        [MaxLength(128)] [StringLength(128)] public string UrlPath { get; set; }
        /// <summary>
        /// Version number
        /// </summary>
        [Display(Name = "Article version")] public int VersionNumber { get; set; }
        /// <summary>
        /// Published day and time
        /// </summary>
        [Display(Name = "Publish on date/time (PST):")]
        [DataType(DataType.DateTime)]
        public DateTime? Published { get; set; }
        /// <summary>
        /// Article title
        /// </summary>
        [MaxLength(80)]
        [StringLength(80)]
        [Display(Name = "Article title")]
        public string Title { get; set; }
        /// <summary>
        /// Date and time last updated
        /// </summary>
        [Display(Name = "Article last saved")] public DateTime Updated { get; set; }
    }
}