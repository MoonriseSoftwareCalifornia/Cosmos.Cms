using CDT.Cosmos.Cms.Common.Data.Logic;
using CDT.Cosmos.Cms.Common.Models.Attributes;
using CDT.Cosmos.Cms.Common.Services.Configurations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.ComponentModel.DataAnnotations;

namespace CDT.Cosmos.Cms.Common.Models
{
    /// <summary>
    ///     Article view model, used to display content on a web page
    /// </summary>
    [Serializable]
    public class ArticleViewModel
    {
        /// <summary>
        ///     Entity key for the article
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        ///     Status code of the article
        /// </summary>
        public StatusCodeEnum StatusCode { get; set; }

        /// <summary>
        ///     Article number
        /// </summary>
        public int ArticleNumber { get; set; }

        /// <summary>
        ///     ISO language code of this article
        /// </summary>
        public string LanguageCode { get; set; } = "en";

        /// <summary>
        ///     Language name
        /// </summary>
        public string LanguageName { get; set; } = "English";

        /// <summary>
        ///     Url of this page
        /// </summary>
        [MaxLength(128)]
        [StringLength(128)]
        public string UrlPath { get; set; }

        /// <summary>
        ///     Version number of this article
        /// </summary>
        [Display(Name = "Article version")]
        public int VersionNumber { get; set; }

        /// <summary>
        ///     Article title
        /// </summary>
        [MaxLength(80)]
        [StringLength(80)]
        [Display(Name = "Article title")]
        [ArticleTitleValidation]
        [Remote("CheckTitle", "Edit", AdditionalFields = "ArticleNumber")]
        public string Title { get; set; }

        /// <summary>
        ///     HTML Content of the page
        /// </summary>
        [DataType(DataType.Html)]
        public string Content { get; set; }

        /// <summary>
        /// JavaScript block injected into HEAD for this particular page (article)
        /// </summary>
        [DataType(DataType.Html)]
        public string HeadJavaScript { get; set; }

        /// <summary>
        ///     JavaScript block injected into the footer
        /// </summary>
        [DataType(DataType.Html)]
        public string FooterJavaScript { get; set; }

        /// <summary>
        ///     Layout used by this page.
        /// </summary>
        public LayoutViewModel Layout { get; set; }

        /// <summary>
        ///     Roles allowed to view this page.
        /// </summary>
        /// <remarks>If this value is null, it assumes page can be viewed anonymously.</remarks>
        public string RoleList { get; set; }

        #region DATETIME FIELDS

        /// <summary>
        ///     Date and time of when this article was last updated.
        /// </summary>
        [Display(Name = "Article last saved")]
        public virtual DateTime Updated { get; set; }

        /// <summary>
        ///     Date and time of when this was published
        /// </summary>
        [Display(Name = "Publish on date/time (PST):")]
        [DataType(DataType.DateTime)]
        [DateTimeUtcKind]
        public virtual DateTime? Published { get; set; }

        /// <summary>
        ///     If set, is the date/time when this version of the article expires.
        /// </summary>
        /// <remarks>
        ///     This is calculated based on either this article's expiration date, or, the default cache duration set in <see cref="RedisConfig.CacheDuration"/> found in <see cref="CosmosConfig.RedisConfig"/>.
        /// </remarks>
        [Display(Name = "Expires on (UTC):")]
        [DataType(DataType.DateTime)]
        public virtual DateTime? Expires { get; set; }

        #endregion

        #region MODE SPECIFIC

        /// <summary>
        ///     Indicates if this is in authoring (true) or publishing (false) mode, Default is false.
        /// </summary>
        /// <remarks>
        ///     Is the value set by <see cref="SiteSettings.ReadWriteMode" /> which
        ///     is set in Startup and injected into controllers using <see cref="IOptions{TOptions}" />.
        /// </remarks>
        public bool ReadWriteMode { get; set; } = false;

        /// <summary>
        ///     Indicates is page is in preview model. Default is false.
        /// </summary>
        public bool PreviewMode { get; set; } = false;

        /// <summary>
        ///     Indicates if page is in edit, or authoring mode. Default is false.
        /// </summary>
        public bool EditModeOn { get; set; } = false;

        /// <summary>
        ///     Cache key used by REDIS
        /// </summary>
        public string CacheKey { get; set; }

        /// <summary>
        ///     Cache during for this article
        /// </summary>
        /// <remarks>
        /// Calculated using the expires value (if present) or the default set with <see cref="RedisConfig.CacheDuration" in <see cref="CosmosConfig.RedisConfig"/>./>
        /// </remarks>
        public int CacheDuration { get; set; }

        #endregion
    }
}