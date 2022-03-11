using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CDT.Cosmos.Cms.Common.Services.Configurations
{
    /// <summary>
    ///     Cosmos configuration model
    /// </summary>
    public class CosmosConfig
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        public CosmosConfig()
        {
            AuthenticationConfig = new AuthenticationConfig();
            CdnConfig = new CdnConfig();
            SqlConnectionStrings = new List<SqlConnectionString>();
            EditorUrls = new List<EditorUrl>();
            SiteSettings = new SiteSettings();
            StorageConfig = new StorageConfig();
            SendGridConfig = new SendGridConfig();
            MicrosoftAppId = string.Empty;
        }

        /// <summary>
        ///     Primary cloud for this installation.
        /// </summary>
        [Display(Name = "Primary Cloud")]
        [UIHint("CloudProvider")]
        public string PrimaryCloud { get; set; }

        /// <summary>
        /// Primary ISO-639-1 language code for this website.
        /// </summary>
        /// <remarks>
        /// <para>Default value is "en-US".</para>
        /// <para>
        /// When Google translate is configured and a page translation is requested, the
        /// translation is based on what language code is requested.  To see a list of
        /// codes see <see href="https://cloud.google.com/translate/docs/languages">Google's
        /// list of supported codes.</see>
        /// </para>
        /// </remarks>
        [Display(Name = "Primary language code.")]
        public string PrimaryLanguageCode { get; set; } = "en-US";

        /// <summary>
        ///     Authentication
        /// </summary>
        public AuthenticationConfig AuthenticationConfig { get; set; }

        /// <summary>
        ///     CDN Configuration
        /// </summary>
        public CdnConfig CdnConfig { get; set; }

        /// <summary>
        ///     Editor Urls
        /// </summary>
        public List<EditorUrl> EditorUrls { get; set; }

        /// <summary>
        ///     Database connection strings
        /// </summary>
        public List<SqlConnectionString> SqlConnectionStrings { get; set; }

        /// <summary>
        ///     Google Cloud service authentication configuration
        /// </summary>
        public GoogleCloudAuthConfig GoogleCloudAuthConfig { get; set; }

        /// <summary>
        /// Microsoft application ID used for application verification.
        /// </summary>
        public string MicrosoftAppId { get; set; }

        /// <summary>
        ///     SendGrid configuration
        /// </summary>
        public SendGridConfig SendGridConfig { get; set; }

        /// <summary>
        ///     Site-wide settings
        /// </summary>
        public SiteSettings SiteSettings { get; set; }

        /// <summary>
        ///     Blob service configuration
        /// </summary>
        public StorageConfig StorageConfig { get; set; }

        /// <summary>
        ///     Environment Variable Name
        /// </summary>
        public string SecretName { get; set; } = "";

        /// <summary>
        ///     Secret key used for JWT authenticated communication between editors.
        /// </summary>
        [RegularExpression(@"^[0-9, a-z, A-Z]{32,32}$",
            ErrorMessage = "Must have at least 32 random numbers and letters.")]
        public string SecretKey { get; set; }
    }
}