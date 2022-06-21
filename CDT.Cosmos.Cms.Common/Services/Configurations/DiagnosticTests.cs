using Amazon;
using Amazon.S3;
using Azure.Storage.Blobs;
using CDT.Cosmos.Cms.Common.Data;
using Microsoft.Azure.Management.Cdn;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Rest;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CDT.Cosmos.Cms.Common.Services.Configurations.BootUp
{
    /// <summary>
    /// Boot time service diagnostics results.
    /// </summary>
    public class DiagnosticTests
    {
        public const string AUTHSERVICETYPENAME = "Authentication";
        public const string BLOBSERVICETYPENAME = "Storage";
        public const string BOOTVARSSERVICETYPENAME = "Boot Variables";
        public const string CDNSERVICETYPENAME = "CDN";
        public const string DBSERVICETYPENAME = "DB";
        public const string REDISSERVICETYPENAME = "Redis";
        public const string SENDGRIDSERVICETYPENAME = "SendGrid";
        public const string SITESETTINGSSERVICETYPENAME = "Site Settings";
        public const string TRANSLATIONSSERVICETYPENAME = "Translations";
        public const string STARTUPSERVICETYPENAME = "Startup";

        private readonly IOptions<CosmosConfig> _config;
        private readonly List<Diagnostic> _diagnostics = new List<Diagnostic>();

        /// <summary>
        /// Startup is error free.
        /// </summary>
        private bool _startupErrorFree = true;

        #region MINIMUM FOR PUBLISHER TO RUN
        private bool _sqlConfigured = false;
        #endregion

        #region MINIMUM FOR EDITOR TO RUN
        // Also includes minimum for publisher
        private bool _storageConfigured = false;
        private bool _sendGridConfigured = false;
        private bool _publisherUrlConfigured = false;
        private bool _blobPublicUrlConfigured = false;
        private bool _allowedFileTypesConfigured = false;
        private bool _secretKeyExists = false;
        #endregion

        /// <summary>
        /// Adds a diagnostic to the existing telemtry set.
        /// </summary>
        /// <param name="telemetry"></param>
        public void AddStarupDiagnostic(Diagnostic telemetry)
        {
            _startupErrorFree = false;
            _diagnostics.Add(telemetry);
        }

        /// <summary>
        /// Gets the diagnostics results.
        /// </summary>
        /// <returns></returns>
        public List<Diagnostic> GetDiagnostics()
        {
            return _diagnostics;
        }

        /// <summary>
        /// Configuration is sufficient for editor
        /// </summary>
        /// <remarks>Indicates that SQL, Storage and SendGrid are validated.</remarks>
        public bool EditorIsConfigured
        {
            get
            {
                return (_startupErrorFree && PublisherIsConfigured && _storageConfigured && _sendGridConfigured &&
                  _publisherUrlConfigured && _blobPublicUrlConfigured && _allowedFileTypesConfigured && _secretKeyExists);
            }
        }

        /// <summary>
        /// Configuration is sufficient for publisher 
        /// </summary>
        /// <remarks>Indicates that SQL is configured and is current on its migrations.</remarks>
        public bool PublisherIsConfigured
        {
            get { return _startupErrorFree && _sqlConfigured; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="config"></param>
        public DiagnosticTests(IOptions<CosmosConfig> config)
        {
            _config = config;
        }

        /// <summary>
        /// Runs all diagnostics related to Cosmos configuration.
        /// </summary>
        /// <param name="includeStorage"></param>
        /// <param name="includeSendGrid"></param>
        /// <param name="includeRedis"></param>
        /// <param name="includeCdn"></param>
        /// <param name="includeTranslate"></param>
        /// <returns></returns>
        /// <remarks>
        /// <para>Runs the following tests:</para>
        /// <list type="number">
        /// <item><see cref="Check_ConfigValueIsNotNull(List{Diagnostic})"/></item>
        /// <item><see cref="Check_SiteSettings()"/></item>
        /// <item><see cref="Check_Authentication()"/></item>
        /// <item><see cref="Check_Sql()"/></item>
        /// <item><see cref="Check_Storage()"/></item>
        /// <item><see cref="Check_SendGrid()"/></item>
        /// <item><see cref="Check_Redis()"/></item>
        /// <item><see cref="Check_Cdn()"/></item>
        /// <item><see cref="Check_GoogleTranslate()"/></item>
        /// </list>
        /// </remarks>
        public async Task<List<Diagnostic>> Run()
        {
            if (!Check_ConfigValueIsNotNull(_diagnostics))
            {
                return _diagnostics;
            }
            _diagnostics.AddRange(Check_SiteSettings());
            _diagnostics.AddRange(Check_Authentication());
            _diagnostics.AddRange(await Check_Sql());
            _diagnostics.AddRange(await Check_Storage());
            _diagnostics.AddRange(await Check_SendGrid());
            _diagnostics.AddRange(await Check_Cdn());
            _diagnostics.AddRange(await Check_GoogleTranslate());

            return _diagnostics;
        }

        /// <summary>
        /// Check if Cosmos options value is null.
        /// </summary>
        /// <returns></returns>
        private bool Check_ConfigValueIsNotNull(List<Diagnostic> diagnostics)
        {
            if (_config.Value == null)
            {
                diagnostics.Add(new Diagnostic()
                {
                    ServiceType = BOOTVARSSERVICETYPENAME,
                    Success = false,
                    Message = "Cosmos options value is null."
                });
                return false;
            }
            return true;
        }

        /// <summary>
        /// Checks for the minimum site settings needed for both Editor and Publisher
        /// </summary>
        /// <returns></returns>
        /// <remarks>This checks for the existence of AllowsFileTypes, BlobPublicUrl, and PublisherUrl</remarks>
        public List<Diagnostic> Check_SiteSettings()
        {
            var diagnostics = new List<Diagnostic>();

            if (Check_ConfigValueIsNotNull(diagnostics))
            {
                if (string.IsNullOrEmpty(_config.Value.SecretKey))
                {
                    diagnostics.Add(new Diagnostic()
                    {
                        ServiceType = SITESETTINGSSERVICETYPENAME,
                        Success = false,
                        Message = "Secret key is null."
                    });
                }
                else if (_config.Value.SecretKey.Length < 32)
                {
                    diagnostics.Add(new Diagnostic()
                    {
                        ServiceType = SITESETTINGSSERVICETYPENAME,
                        Success = false,
                        Message = "Secret key is less than 32 characters in length."
                    });
                }
                else
                {
                    _secretKeyExists = true;

                    diagnostics.Add(new Diagnostic()
                    {
                        ServiceType = SITESETTINGSSERVICETYPENAME,
                        Success = true,
                        Message = "Secret key is set."
                    });
                }

                if (_config.Value.SiteSettings == null || string.IsNullOrEmpty(_config.Value.SiteSettings.AllowedFileTypes))
                {
                    diagnostics.Add(new Diagnostic()
                    {
                        ServiceType = SITESETTINGSSERVICETYPENAME,
                        Success = false,
                        Message = "Allowed File Types variable is null."
                    });
                }
                else
                {
                    _allowedFileTypesConfigured = true;
                    diagnostics.Add(new Diagnostic()
                    {
                        ServiceType = SITESETTINGSSERVICETYPENAME,
                        Success = true,
                        Message = $"Allowed file types set to: { _config.Value.SiteSettings.AllowedFileTypes }."
                    });
                }

                if (_config.Value.SiteSettings == null || string.IsNullOrEmpty(_config.Value.SiteSettings.BlobPublicUrl))
                {
                    diagnostics.Add(new Diagnostic()
                    {
                        ServiceType = SITESETTINGSSERVICETYPENAME,
                        Success = false,
                        Message = "File (BLOB) storage URL is missing."
                    });
                }
                else
                {
                    _blobPublicUrlConfigured = true;
                    diagnostics.Add(new Diagnostic()
                    {
                        ServiceType = SITESETTINGSSERVICETYPENAME,
                        Success = true,
                        Message = $"File (BLOB) storage URL set to: { _config.Value.SiteSettings.BlobPublicUrl }."
                    });
                }

                if (_config.Value.SiteSettings == null || string.IsNullOrEmpty(_config.Value.SiteSettings.PublisherUrl))
                {
                    diagnostics.Add(new Diagnostic()
                    {
                        ServiceType = SITESETTINGSSERVICETYPENAME,
                        Success = false,
                        Message = "Publisher URL is missing."
                    });
                }
                else
                {
                    _publisherUrlConfigured = true;
                    diagnostics.Add(new Diagnostic()
                    {
                        ServiceType = SITESETTINGSSERVICETYPENAME,
                        Success = true,
                        Message = $"File (BLOB) storage URL set to: { _config.Value.SiteSettings.PublisherUrl }."
                    });
                }

                // Optional settings
                if (_config.Value.SiteSettings != null && _config.Value.SiteSettings.AllowSetup.HasValue)
                {
                    var warn = _config.Value.SiteSettings.AllowReset ?? false ? "WARNING! " : "";
                    diagnostics.Add(new Diagnostic()
                    {
                        ServiceType = SITESETTINGSSERVICETYPENAME,
                        Success = true,
                        Message = $"{warn}Allow Setup set to: { _config.Value.SiteSettings.AllowSetup.Value }."
                    });
                }

                // Optional settings
                if (_config.Value.SiteSettings != null && _config.Value.SiteSettings.AllowReset.HasValue && _config.Value.SiteSettings.AllowReset.Value)
                {
                    var warn = _config.Value.SiteSettings.AllowReset ?? false ? "WARNING! " : "";
                    diagnostics.Add(new Diagnostic()
                    {
                        ServiceType = SITESETTINGSSERVICETYPENAME,
                        Success = true,
                        Message = $"{warn}Allow Reset set to: { _config.Value.SiteSettings.AllowReset }."
                    });
                }
            }

            return diagnostics;
        }

        public List<Diagnostic> Check_Authentication()
        {
            var diagnostics = new List<Diagnostic>();

            if (Check_ConfigValueIsNotNull(diagnostics))
            {
                if (_config.Value.AuthenticationConfig == null)
                {
                    diagnostics.Add(new Diagnostic()
                    {
                        ServiceType = AUTHSERVICETYPENAME,
                        Success = true,
                        Message = "Authentication Config is nul."
                    });
                }
                else
                {
                    // TODO: Add checks for OAuth providers.
                }
            }

            return diagnostics;
        }

        /// <summary>
        /// Runs diagnostics for all configured databases.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// <para>Checks to see if at least one SQL database is configured and if Cosmos can connect, and if any pending EF migrations exist.</para>
        /// </remarks>
        public async Task<List<Diagnostic>> Check_Sql()
        {
            var diagnostics = new List<Diagnostic>();

            if (Check_ConfigValueIsNotNull(diagnostics))
            {
                if (_config.Value.SqlConnectionStrings.Any())
                {
                    foreach (var sqlConnection in _config.Value.SqlConnectionStrings)
                    {
                        try
                        {
                            var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
                            builder.UseSqlServer(sqlConnection.ToString());

                            await using (var dbContext = new ApplicationDbContext(builder.Options))
                            {
                                if (await dbContext.Database.CanConnectAsync())
                                {
                                    diagnostics.Add(new Diagnostic()
                                    {
                                        ServiceType = DBSERVICETYPENAME,
                                        Success = true,
                                        Message = $"Can connect to: { sqlConnection.Hostname }."
                                    });

                                    var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();

                                    if (pendingMigrations.Any())
                                    {
                                        diagnostics.Add(new Diagnostic()
                                        {
                                            ServiceType = DBSERVICETYPENAME,
                                            Success = false,
                                            Message = $"Migrations need to be applid to: { sqlConnection.Hostname }\\{sqlConnection.InitialCatalog}."
                                        });
                                        foreach (var migration in pendingMigrations)
                                        {
                                            diagnostics.Add(new Diagnostic()
                                            {
                                                ServiceType = DBSERVICETYPENAME,
                                                Success = false,
                                                Message = $"Migration: { migration }."
                                            });
                                        }
                                    }
                                }
                                else
                                {
                                    diagnostics.Add(new Diagnostic()
                                    {
                                        ServiceType = DBSERVICETYPENAME,
                                        Success = false,
                                        Message = $"Could not connect to DB server: { sqlConnection.Hostname }."
                                    });
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            diagnostics.Add(new Diagnostic()
                            {
                                ServiceType = DBSERVICETYPENAME,
                                Success = false,
                                Message = $"Error while trying to access: { sqlConnection.Hostname }. Error message: { e.Message }."
                            });
                        }
                    }
                }
                else
                {
                    diagnostics.Add(new Diagnostic()
                    {
                        ServiceType = DBSERVICETYPENAME,
                        Success = false,
                        Message = $"No SQL connections found."
                    });
                }
            }

            _sqlConfigured = diagnostics.All(a => a.Success);
            return diagnostics;
        }

        /// <summary>
        /// Runs diagnostics for blob storage accounts.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// <para>Checks to see if at least one storage account is configured and that Cosmos can connect.</para>
        /// </remarks>
        public async Task<List<Diagnostic>> Check_Storage()
        {
            var diagnostics = new List<Diagnostic>();

            if (Check_ConfigValueIsNotNull(diagnostics))
            {
                if (_config.Value.StorageConfig.AmazonConfigs.Any() ||
                    _config.Value.StorageConfig.AzureConfigs.Any())
                {
                    // Azure Blob Connections
                    foreach (var storageConn in _config.Value.StorageConfig.AzureConfigs)
                    {
                        try
                        {
                            var client = new BlobServiceClient(storageConn.AzureBlobStorageConnectionString);
                            var containers = client.GetBlobContainerClient("$web");

                            var properties = await containers.GetPropertiesAsync();

                            diagnostics.Add(new Diagnostic()
                            {
                                ServiceType = BLOBSERVICETYPENAME,
                                Success = true,
                                Message = $"Can connect to: { storageConn.AzureBlobStorageEndPoint }."
                            });
                        }
                        catch (Exception e)
                        {
                            diagnostics.Add(new Diagnostic()
                            {
                                ServiceType = BLOBSERVICETYPENAME,
                                Success = false,
                                Message = $"Error connecting to: { storageConn.AzureBlobStorageEndPoint }.  Error message: { e.Message }."
                            });
                        }
                    }

                    // Azure Blob Connections
                    foreach (var storageConn in _config.Value.StorageConfig.AmazonConfigs)
                    {
                        try
                        {
                            var regionIdentifier = RegionEndpoint.GetBySystemName(storageConn.AmazonRegion);
                            using var client = new AmazonS3Client(storageConn.AmazonAwsAccessKeyId,
                                storageConn.AmazonAwsSecretAccessKey, regionIdentifier);
                            var bucketList = await client.ListBucketsAsync();
                            diagnostics.Add(new Diagnostic()
                            {
                                ServiceType = BLOBSERVICETYPENAME,
                                Success = true,
                                Message = $"Can connect to: { storageConn.ServiceUrl }."
                            });
                        }
                        catch (Exception e)
                        {
                            diagnostics.Add(new Diagnostic()
                            {
                                ServiceType = BLOBSERVICETYPENAME,
                                Success = false,
                                Message = $"Error connecting to: { storageConn.ServiceUrl }.  Error message: { e.Message }."
                            });
                        }

                    }

                }
                else
                {
                    diagnostics.Add(new Diagnostic()
                    {
                        ServiceType = BLOBSERVICETYPENAME,
                        Success = false,
                        Message = $"No storage connections found."
                    });
                }
            }

            _storageConfigured = diagnostics.All(a => a.Success);

            return diagnostics;
        }

        /// <summary>
        /// Check SendGrid connection
        /// </summary>
        /// <returns></returns>
        /// <remarks>Ensures the existance of a SendGrid configuration and that Cosmos can connect to it.</remarks>
        public async Task<List<Diagnostic>> Check_SendGrid()
        {
            var diagnostics = new List<Diagnostic>();

            if (Check_ConfigValueIsNotNull(diagnostics))
            {
                if (_config.Value.SendGridConfig == null || string.IsNullOrEmpty(_config.Value.SendGridConfig.SendGridKey))
                {
                    diagnostics.Add(new Diagnostic()
                    {
                        ServiceType = SENDGRIDSERVICETYPENAME,
                        Success = false,
                        Message = $"No SendGrid connections found."
                    });
                }
                else
                {
                    var client = new SendGridClient(_config.Value.SendGridConfig.SendGridKey);
                    var msg = new SendGridMessage
                    {
                        From = new EmailAddress("eric.kauffman@state.ca.gov"),
                        Subject = "Test Email",
                        PlainTextContent = "Hello World!",
                        HtmlContent = "<p>Hello World!</p>"
                    };
                    msg.AddTo("eric.kauffman@state.ca.gov");

                    msg.MailSettings = new MailSettings();
                    msg.MailSettings.SandboxMode = new SandboxMode { Enable = true };

                    var diagnostic = new Diagnostic();
                    diagnostic.ServiceType = SENDGRIDSERVICETYPENAME;

                    try
                    {
                        var result = await client.SendEmailAsync(msg);
                        switch (result.StatusCode)
                        {
                            case HttpStatusCode.OK:
                                _sendGridConfigured = true;
                                diagnostic.Success = true;
                                diagnostic.Message = "SendGrid account is configured.";
                                break;
                            case HttpStatusCode.Unauthorized:
                                diagnostic.Success = false;
                                diagnostic.Message = "SendGrid test returns 'Unauthorized.'";
                                break;
                            default:
                                diagnostic.Success = false;
                                diagnostic.Message = $"SendGrid error: { result.Body }.";
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        diagnostic.Success = false;
                        diagnostic.Message = $"SendGrid test error: { e.Message }.";
                    }
                }
            }

            return diagnostics;
        }

        /// <summary>
        /// Check CDN connection
        /// </summary>
        /// <returns></returns>
        /// <remarks>Checks to see if a CDN is configured and if Cosmos can connect to manage it.</remarks>
        public async Task<List<Diagnostic>> Check_Cdn()
        {
            var diagnostics = new List<Diagnostic>();

            if (Check_ConfigValueIsNotNull(diagnostics))
            {
                if (_config.Value.CdnConfig == null)
                {
                    diagnostics.Add(new Diagnostic()
                    {
                        Message = "CDN configuration is null.",
                        ServiceType = CDNSERVICETYPENAME,
                        Success = false
                    });
                }
                else
                {
                    if (_config.Value.CdnConfig.AzureCdnConfig != null)
                    {
                        if (!string.IsNullOrEmpty(_config.Value.CdnConfig.AzureCdnConfig.ClientId)
                            || !string.IsNullOrEmpty(_config.Value.CdnConfig.AzureCdnConfig.CdnProvider)
                            || !string.IsNullOrEmpty(_config.Value.CdnConfig.AzureCdnConfig.ClientSecret)
                            || !string.IsNullOrEmpty(_config.Value.CdnConfig.AzureCdnConfig.TenantId)
                            || !string.IsNullOrEmpty(_config.Value.CdnConfig.AzureCdnConfig.TenantDomainName)
                            || !string.IsNullOrEmpty(_config.Value.CdnConfig.AzureCdnConfig.CdnProfileName)
                            || !string.IsNullOrEmpty(_config.Value.CdnConfig.AzureCdnConfig.EndPointName)
                            || !string.IsNullOrEmpty(_config.Value.CdnConfig.AzureCdnConfig.ResourceGroup)
                            || !string.IsNullOrEmpty(_config.Value.CdnConfig.AzureCdnConfig.SubscriptionId)
                        )
                        {
                            // Check for missing fields.
                            if (string.IsNullOrEmpty(_config.Value.CdnConfig.AzureCdnConfig.ClientId)
                                || string.IsNullOrEmpty(_config.Value.CdnConfig.AzureCdnConfig.CdnProvider)
                                || string.IsNullOrEmpty(_config.Value.CdnConfig.AzureCdnConfig.ClientSecret)
                                || string.IsNullOrEmpty(_config.Value.CdnConfig.AzureCdnConfig.TenantId)
                                || string.IsNullOrEmpty(_config.Value.CdnConfig.AzureCdnConfig.TenantDomainName)
                                || string.IsNullOrEmpty(_config.Value.CdnConfig.AzureCdnConfig.CdnProfileName)
                                || string.IsNullOrEmpty(_config.Value.CdnConfig.AzureCdnConfig.EndPointName)
                                || string.IsNullOrEmpty(_config.Value.CdnConfig.AzureCdnConfig.ResourceGroup)
                                || string.IsNullOrEmpty(_config.Value.CdnConfig.AzureCdnConfig.SubscriptionId)
                            )
                            {
                                diagnostics.Add(new Diagnostic()
                                {
                                    Message = "Azure CDN settings are incomplete.",
                                    ServiceType = CDNSERVICETYPENAME,
                                    Success = false
                                });
                            }
                            else
                            {
                                try
                                {
                                    //var authority = $"https://login.microsoftonline.com/{_config.Value.CdnConfig.AzureCdnConfig.TenantId }/{_config.Value.CdnConfig.AzureCdnConfig.TenantDomainName }";

                                    //var authContext = new AuthenticationContext(authority);

                                    //var credential = new ClientCredential(_config.Value.CdnConfig.AzureCdnConfig.ClientId, _config.Value.CdnConfig.AzureCdnConfig.ClientSecret);
                                    //var tokenResult = await authContext.AcquireTokenAsync("https://management.core.windows.net/", credential);

                                    var scope = "https://management.core.windows.net//.default";
                                    var app = ConfidentialClientApplicationBuilder.Create(_config.Value.CdnConfig.AzureCdnConfig.ClientId)
                                        .WithClientSecret(_config.Value.CdnConfig.AzureCdnConfig.ClientSecret)
                                        .WithTenantId(_config.Value.CdnConfig.AzureCdnConfig.TenantId).Build();

                                    var authResult = await app.AcquireTokenForClient(new[] { scope }).ExecuteAsync();

                                    using var client = new CdnManagementClient(new TokenCredentials(authResult.AccessToken))
                                    {
                                        SubscriptionId = _config.Value.CdnConfig.AzureCdnConfig.SubscriptionId
                                    };

                                    if (authResult.TokenType == "Bearer")
                                    {
                                        var profiles = await client.Profiles.ListWithHttpMessagesAsync();
                                        if (profiles != null)
                                        {
                                            diagnostics.Add(new Diagnostic()
                                            {
                                                Message = "Connection to Azure CDN succeeded.",
                                                ServiceType = CDNSERVICETYPENAME,
                                                Success = true
                                            });
                                        }
                                        else
                                        {
                                            diagnostics.Add(new Diagnostic()
                                            {
                                                Message = "Azure CDN endpoint connection failed.",
                                                ServiceType = CDNSERVICETYPENAME,
                                                Success = false
                                            });
                                        }
                                    }
                                    else
                                    {
                                        diagnostics.Add(new Diagnostic()
                                        {
                                            Message = "Azure CDN endpoint connection failed.",
                                            ServiceType = CDNSERVICETYPENAME,
                                            Success = false
                                        });
                                    }

                                }
                                catch (Exception e)
                                {
                                    diagnostics.Add(new Diagnostic()
                                    {
                                        ServiceType = REDISSERVICETYPENAME,
                                        Success = false,
                                        Message = $"Error connecting to Azure CDN.  Error message: { e.Message }."
                                    });
                                }
                            }
                        }
                    }
                    else if (_config.Value.CdnConfig.AkamaiContextConfig != null)
                    {
                        // TODO: Check Akamai settings.
                    }

                }
            }

            return diagnostics;
        }

        /// <summary>
        /// Check Google Translate Connection
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// Checks for a Google Translate configuration and that Cosmos can connect to it.
        /// </remarks>
        public async Task<List<Diagnostic>> Check_GoogleTranslate()
        {
            var diagnostics = new List<Diagnostic>();

            if (_config.Value.GoogleCloudAuthConfig == null)
            {
                diagnostics.Add(new Diagnostic()
                {
                    Message = "Google authentication config is null.",
                    ServiceType = TRANSLATIONSSERVICETYPENAME,
                    Success = true
                });
                return diagnostics;
            }

            if (Check_ConfigValueIsNotNull(diagnostics))
            {
                // Is the configuration started?
                if (!string.IsNullOrEmpty(_config.Value.GoogleCloudAuthConfig.ClientId)
                || !string.IsNullOrEmpty(_config.Value.GoogleCloudAuthConfig.AuthProviderX509CertUrl)
                || !string.IsNullOrEmpty(_config.Value.GoogleCloudAuthConfig.AuthUri)
                || !string.IsNullOrEmpty(_config.Value.GoogleCloudAuthConfig.ClientEmail)
                || !string.IsNullOrEmpty(_config.Value.GoogleCloudAuthConfig.ServiceType)
                || !string.IsNullOrEmpty(_config.Value.GoogleCloudAuthConfig.ProjectId)
                || !string.IsNullOrEmpty(_config.Value.GoogleCloudAuthConfig.PrivateKeyId)
                || !string.IsNullOrEmpty(_config.Value.GoogleCloudAuthConfig.PrivateKey)
                || !string.IsNullOrEmpty(_config.Value.GoogleCloudAuthConfig.TokenUri)
                || !string.IsNullOrEmpty(_config.Value.GoogleCloudAuthConfig.ClientX509CertificateUrl)
            )
                {

                    // Check for missing fields.
                    if (string.IsNullOrEmpty(_config.Value.GoogleCloudAuthConfig.ClientId)
                        || string.IsNullOrEmpty(_config.Value.GoogleCloudAuthConfig.AuthProviderX509CertUrl)
                        || string.IsNullOrEmpty(_config.Value.GoogleCloudAuthConfig.AuthUri)
                        || string.IsNullOrEmpty(_config.Value.GoogleCloudAuthConfig.ClientEmail)
                        || string.IsNullOrEmpty(_config.Value.GoogleCloudAuthConfig.ServiceType)
                        || string.IsNullOrEmpty(_config.Value.GoogleCloudAuthConfig.ProjectId)
                        || string.IsNullOrEmpty(_config.Value.GoogleCloudAuthConfig.PrivateKeyId)
                        || string.IsNullOrEmpty(_config.Value.GoogleCloudAuthConfig.PrivateKey)
                        || string.IsNullOrEmpty(_config.Value.GoogleCloudAuthConfig.TokenUri)
                        || string.IsNullOrEmpty(_config.Value.GoogleCloudAuthConfig.ClientX509CertificateUrl)
                    )
                    {
                        diagnostics.Add(new Diagnostic()
                        {
                            Message = "Google authentication is configured.",
                            ServiceType = TRANSLATIONSSERVICETYPENAME,
                            Success = true
                        });
                    }
                    else
                    {
                        try
                        {
                            var translationServices = new TranslationServices(_config);

                            var result = await translationServices.GetSupportedLanguages();

                            if (result.Languages.Count > 0)
                            {
                                diagnostics.Add(new Diagnostic()
                                {
                                    Message = "Successfully connected to Google Translate.",
                                    ServiceType = TRANSLATIONSSERVICETYPENAME,
                                    Success = true
                                });
                            }
                            else
                            {
                                diagnostics.Add(new Diagnostic()
                                {
                                    Message = "Failed to connect to Google Translate.",
                                    ServiceType = TRANSLATIONSSERVICETYPENAME,
                                    Success = false
                                });
                            }
                        }
                        catch (Exception e)
                        {
                            diagnostics.Add(new Diagnostic()
                            {
                                ServiceType = TRANSLATIONSSERVICETYPENAME,
                                Success = false,
                                Message = $"Error connecting to Google Translate.  Error message: { e.Message }."
                            });
                        }
                    }
                }
            }

            return diagnostics;
        }

    }
}