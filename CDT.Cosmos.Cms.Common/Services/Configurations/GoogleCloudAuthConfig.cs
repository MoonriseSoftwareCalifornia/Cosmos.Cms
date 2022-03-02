using System.ComponentModel.DataAnnotations;

namespace CDT.Cosmos.Cms.Common.Services.Configurations
{
    /// <summary>
    ///     Google cloud account information
    /// </summary>
    public class GoogleCloudAuthConfig
    {
        private string _parentProjectId = "";
        /// <summary>
        ///     Service type
        /// </summary>
        /// <remarks>Default is "service_account"</remarks>
        [Display(Name = "Service type")]
        public string ServiceType { get; set; } = "service_account";

        /// <summary>
        ///     Project id
        /// </summary>
        /// <remarks>For example "translator-oet"</remarks>
        [Display(Name = "Project Id")]
        public string ProjectId { get; set; }

        /// <summary>
        /// Parent project ID (if there is one)
        /// </summary>
        [Display(Name = "Parent projec Id (if there is one)")]
        public string ParentProjectId { 
            get
            {
                if (string.IsNullOrEmpty(_parentProjectId))
                    return ProjectId;
                return _parentProjectId;
            }
            set 
            {
                _parentProjectId = value;
            }
        }


        /// <summary>
        ///     Private key Id
        /// </summary>
        [Display(Name = "Private Key Id")]
        public string PrivateKeyId { get; set; }

        /// <summary>
        ///     Google account private key
        /// </summary>
        [Display(Name = "Account Private Key")]
        public string PrivateKey { get; set; }

        /// <summary>
        ///     Google account client email
        /// </summary>
        [Display(Name = "Account client email")]
        public string ClientEmail { get; set; }

        /// <summary>
        ///     Client ID
        /// </summary>
        [Display(Name = "Client Id")]
        public string ClientId { get; set; }

        /// <summary>
        ///     Google authentication end point
        /// </summary>
        /// <remarks>Default value is 'https://accounts.google.com/o/oauth2/auth'</remarks>
        [Display(Name = "Authentication end point")]
        public string AuthUri { get; set; } = "https://accounts.google.com/o/oauth2/auth";

        /// <summary>
        ///     Token end point
        /// </summary>
        /// <remarks>Default value is 'https://oauth2.googleapis.com/token'</remarks>
        [Display(Name = "Token end point")]
        public string TokenUri { get; set; } = "https://oauth2.googleapis.com/token";

        /// <summary>
        ///     Authentication provider certificate URL
        /// </summary>
        /// <remarks>Default value is 'https://www.googleapis.com/oauth2/v1/certs'</remarks>
        [Display(Name = "Authentication provider certificate URL")]
        public string AuthProviderX509CertUrl { get; set; } = "https://www.googleapis.com/oauth2/v1/certs";

        /// <summary>
        ///     Client certificate URL
        /// </summary>
        [Display(Name = "Client Certificate URL")]
        public string ClientX509CertificateUrl { get; set; }
    }
}