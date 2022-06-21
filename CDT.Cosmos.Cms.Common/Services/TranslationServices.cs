using CDT.Cosmos.Cms.Common.Services.Configurations;
using Google.Api.Gax.ResourceNames;
using Google.Cloud.Translate.V3;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace CDT.Cosmos.Cms.Common.Services
{
    /// <summary>
    ///     Cosmos CMS Language translation
    /// </summary>
    public class TranslationServices
    {
        private readonly IOptions<CosmosConfig> _config;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="config"></param>
        public TranslationServices(IOptions<CosmosConfig> config)
        {
            if (config.Value != null && config.Value.GoogleCloudAuthConfig != null)
            {
                _config = config;
            }    
        }

        /// <summary>
        ///     Determine if this service is configured
        /// </summary>
        /// <returns></returns>
        public bool IsConfigured()
        {
            return _config.Value.GoogleCloudAuthConfig != null;
        }

        private async Task<TranslationServiceClient> GetTranslatorClient()
        {
            var builder = new TranslationServiceClientBuilder
            {
                CallInvoker = null,
                ChannelCredentials = null,
                CredentialsPath = null,
                Endpoint = null,
                GrpcAdapter = null,
                JsonCredentials = null,
                QuotaProject = null,
                Scopes = null,
                UserAgent = null,
                Settings = null
            };

            var json = "{" +
                       $"\"type\": \"{_config.Value.GoogleCloudAuthConfig.ServiceType}\", " +
                       $" \"project_id\": \"{_config.Value.GoogleCloudAuthConfig.ProjectId}\", " +
                       $" \"private_key_id\": \"{_config.Value.GoogleCloudAuthConfig.PrivateKeyId}\"," +
                       $" \"private_key\": \"{_config.Value.GoogleCloudAuthConfig.PrivateKey}\", " +
                       $" \"client_email\": \"{_config.Value.GoogleCloudAuthConfig.ClientEmail}\", " +
                       $" \"client_id\": \"{_config.Value.GoogleCloudAuthConfig.ClientId}\", " +
                       $" \"auth_uri\": \"{_config.Value.GoogleCloudAuthConfig.AuthUri}\", " +
                       $" \"token_uri\": \"{_config.Value.GoogleCloudAuthConfig.TokenUri}\", " +
                       $" \"auth_provider_x509_cert_url\": \"{_config.Value.GoogleCloudAuthConfig.AuthProviderX509CertUrl}\", " +
                       $" \"client_x509_cert_url\": \"{_config.Value.GoogleCloudAuthConfig.ClientX509CertificateUrl}\"" + "}";

            builder.JsonCredentials = json;

            return await builder.BuildAsync();
        }

        /// <summary>
        ///     Returns content translated into the specified language.
        /// </summary>
        /// <param name="destinationLanguage"></param>
        /// <param name="sourceLanguage"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public async Task<TranslateTextResponse> GetTranslation(string destinationLanguage, string sourceLanguage,
            string[] content)
        {
            var client = await GetTranslatorClient();

            if (string.IsNullOrEmpty(sourceLanguage))
            {
                sourceLanguage = _config.Value.PrimaryLanguageCode;
            }

            var request = new TranslateTextRequest
            {
                SourceLanguageCode = sourceLanguage,
                Contents = { content },
                TargetLanguageCode = destinationLanguage,
                Parent = new ProjectName(_config.Value.GoogleCloudAuthConfig.ParentProjectId).ToString() // Must match .json file
            };
            var response = await client.TranslateTextAsync(request);
            // response.Translations will have one entry, because request.Contents has one entry.
            return response;
        }

        /// <summary>
        ///     Gets a list of supported languages
        /// </summary>
        /// <param name="returnLanguage"></param>
        /// <returns></returns>
        public async Task<SupportedLanguages> GetSupportedLanguages(string returnLanguage = "en")
        {
            var client = await GetTranslatorClient();

            var model = await client.GetSupportedLanguagesAsync(new GetSupportedLanguagesRequest
            {
                DisplayLanguageCode = returnLanguage,
                Parent = new ProjectName(_config.Value.GoogleCloudAuthConfig.ParentProjectId).ToString(),
                ParentAsLocationName = new LocationName(_config.Value.GoogleCloudAuthConfig.ParentProjectId, "us-central1")
            });

            //
            // For convenience, if language is not en, the insert US English first on the list.
            //
            if (!returnLanguage.StartsWith("en"))
                model.Languages.Insert(0, new SupportedLanguage
                {
                    DisplayName = "US English",
                    LanguageCode = "en-US",
                    SupportSource = true,
                    SupportTarget = true
                });
            return model;
        }
    }
}