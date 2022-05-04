using CDT.Cosmos.Cms.Common.Models;
using System;

namespace CDT.Cosmos.Cms.Models
{
    /// <summary>
    ///     Article edit model returned when an article has been saved.
    /// </summary>
    public class ArticleEditViewModel : ArticleViewModel
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="model"></param>
        public ArticleEditViewModel(ArticleViewModel model, CdnPurgeViewModel cdnPurgeModel)
        {
            Id = model.Id;
            StatusCode = model.StatusCode;
            ArticleNumber = model.ArticleNumber;
            LanguageCode = model.LanguageCode;
            LanguageName = model.LanguageName;
            UrlPath = model.UrlPath;
            VersionNumber = model.VersionNumber;
            Published = model.Published;
            Title = model.Title;
            Content = model.Content;
            Updated = model.Updated;
            HeadJavaScript = model.HeadJavaScript;
            FooterJavaScript = model.FooterJavaScript;
            Layout = model.Layout;
            RoleList = model.RoleList;
            ReadWriteMode = model.ReadWriteMode;
            PreviewMode = model.PreviewMode;
            EditModeOn = model.EditModeOn;
            CacheKey = model.CacheKey;
            CacheDuration = model.CacheDuration;
            CdnResult = cdnPurgeModel;
        }

        /// <inheritdoc cref="ArticleViewModel.Published" />
        public new DateTimeOffset? Published
        {
            get => base.Published.HasValue ? base.Published.Value.ToUniversalTime() : null;
            set => base.Published = value.HasValue ? value.Value.UtcDateTime : null;
        }

        /// <inheritdoc cref="ArticleViewModel.Expires" />
        public new DateTimeOffset? Expires
        {
            get => base.Expires.HasValue ? base.Expires.Value.ToUniversalTime() : null;
            set => base.Expires = value.HasValue ? value.Value.UtcDateTime : null;
        }

        /// <inheritdoc cref="ArticleViewModel.Updated" />
        public new DateTimeOffset Updated
        {
            get => base.Updated.ToUniversalTime();
            set => base.Expires = value.UtcDateTime;
        }

        /// <summary>
        ///     CDN Purge Result
        /// </summary>
        public CdnPurgeViewModel CdnResult { get; set; }
    }
}