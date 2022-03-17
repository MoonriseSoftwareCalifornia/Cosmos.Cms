using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDT.Cosmos.Cms.Common.Services.Configurations
{
    /// <summary>
    ///     Common startup configuration builder for Cosmos Startup.cs, Editor or Publisher
    /// </summary>
    public class CosmosOptionsBuilder
    {

        #region PRIVATE PROPERTIES

        private readonly CosmosStartup _cosmosStartup;
        private IConfigurationSection _cosmosConfigSection;

        #endregion

        /// <summary>
        /// Diagnostics results
        /// </summary>
        public List<Diagnostic> Diagnostics { get; } = new();

        /// <summary>
        /// Boot configuration has errors that prevent the Cosmos Config from being built.
        /// </summary>
        public bool HasErrors
        {
            get
            {
                return Diagnostics.Any(a => a.Success == false);
            }
        }

        /// <summary>
        /// Adds as an error log message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="isOk">Is OK or informational, or not an error causing Cosmos not to run</param>
        /// <param name="serviceType"></param>
        public void AddDiagnostic(string message, bool isOk, string serviceType)
        {
            Diagnostics.Add(new Diagnostic()
            {
                Message = message,
                ServiceType = serviceType,
                Success = isOk
            });
        }

        /// <summary>
        ///     Constructor used for unit testing.
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="services"></param>
        /// <param name="cosmosStartup"></param>
        /// <remarks>
        ///  This constructor does not validate settings.
        /// </remarks>
        public CosmosOptionsBuilder(CosmosStartup cosmosStartup)
        {
            _cosmosStartup = cosmosStartup;
        }

        /// <summary>
        ///     Builds the Cosmos configuration from boot parameters.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// <para>Validates run time configuration to ensure the minimum requirements are present:</para>
        /// <list type="bullet">
        ///   <item>SQL Database</item>
        ///   <item>BLOB storage</item>
        /// </list>
        /// <para>When boot parameter AllowSetup is set to true, runs boot time diagnostics.</para>
        /// </remarks>
        public IOptions<CosmosConfig> Build(IConfiguration configuration)
        {

            if (_cosmosStartup.UseAzureVault)
            {
                //
                // Get the C/CMS configuration from Azure Vault
                //
                SecretClient secretClient;
                var builder = new ConfigurationBuilder();
                if (_cosmosStartup.UseDefaultCredential)
                {
                    try
                    {
                        AddDiagnostic($"Creating secret client using default credentials and vault '{_cosmosStartup.CosmosAzureVaultUrl}'.", true, "Secret Client");
                        secretClient = new SecretClient(new Uri(_cosmosStartup.CosmosAzureVaultUrl),
                            new DefaultAzureCredential());
                    }
                    catch (Exception e)
                    {
                        AddDiagnostic("Failed creating secret client with error: " + e.Message, false, "Secret Client");
                    }
                }
                else
                {
                    try
                    {
                        AddDiagnostic($"Creating secret client using tenant ID, client Id, Client secret and vault '{_cosmosStartup.CosmosAzureVaultUrl}'.", true, "Secret Client");

                        secretClient = new SecretClient(new Uri(_cosmosStartup.CosmosAzureVaultUrl),
                            new ClientSecretCredential(
                                _cosmosStartup.AzureVaultTenantId,
                                _cosmosStartup.AzureVaultClientId,
                                _cosmosStartup.AzureVaultClientSecret));
                        builder.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());
                    }
                    catch (Exception e)
                    {
                        AddDiagnostic("Failed creating secret client with error: " + e.Message, false, "Secret Client");
                    }
                }

                try
                {
                    AddDiagnostic($"Attempting to build configuration by connecting to Azure Vault: '{_cosmosStartup.CosmosAzureVaultUrl}'.", true, "Azure Vault");

                    var cosmosConfiguration = Retry.Do(() => builder.Build(), TimeSpan.FromSeconds(1));

                    if (!string.IsNullOrEmpty(_cosmosStartup.SecretName))
                        _cosmosConfigSection = cosmosConfiguration.GetSection(_cosmosStartup.SecretName);

                    AddDiagnostic($"Retrieved configuration section '{_cosmosStartup.SecretName}'.", true, "Azure Vault");

                }
                catch (Exception e)
                {
                    AddDiagnostic("Failed build configuration that includes Azure Vault: " + e.Message, false, "Azure Vault");
                }
            }
            else if (_cosmosStartup.UseAwsSecretsMgr)
            {
                try
                {
                    AddDiagnostic($"Attempting to connect to AWS Secrets Manager.", true, "AWS Secrets Manager");

                    var json = GetAwsSecret(_cosmosStartup.SecretName, _cosmosStartup.AwsKeyId,
                        _cosmosStartup.AwsSecretAccessKey, _cosmosStartup.AwsSecretsRegion).Result;

                    if (string.IsNullOrEmpty(json))
                    {
                        AddDiagnostic($"Variable {_cosmosStartup.SecretName} in AWS Secrets Manager is null or empty.", false, "AWS Secrets Manager");
                    }
                    else
                    {
                        AddDiagnostic($"Variable {_cosmosStartup.SecretName} retrieved value. Adding json to config.", true, "AWS Secrets Manager");

                        using var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(json));

                        var builder = new ConfigurationBuilder();
                        builder.AddJsonStream(memoryStream);
                        var rootConfig = builder.Build();
                        _cosmosConfigSection = rootConfig.GetSection(_cosmosStartup.SecretName);

                        AddDiagnostic($"Retrieved configuration section '{_cosmosStartup.SecretName}'.", true, "AWS Secrets Manager");

                    }
                }
                catch (Exception e)
                {
                    AddDiagnostic("Failed build configuration that includes AWS Secrets Manager: " + e.Message, false, "AWS Secrets Manager");
                }
            }
            else
            {
                //
                // Get the C/CMS configuration from another configuration source (not Azure vault).
                //
                AddDiagnostic($"Attempting to load config from local variable {_cosmosStartup.SecretName}.", true, "Local Secrets");

                if (!string.IsNullOrEmpty(_cosmosStartup.SecretName))
                    _cosmosConfigSection = configuration.GetSection(_cosmosStartup.SecretName);

                AddDiagnostic($"Loaded config from local variable {_cosmosStartup.SecretName}.", true, "Local Secrets");

            }

            // Returns the Cosmos Configuration
            if (HasErrors)
            {
                return null;
            }

            try
            {
                AddDiagnostic("Attempting to load configuration.", true, "Cosmos Config");
                var configOptions = GetCosmosConfig(configuration);
                AddDiagnostic("Successfully loaded configuration.", true, "Cosmos Config");

                return configOptions;
            }
            catch (Exception e)
            {
                AddDiagnostic("Failed to load configuration: " + e.Message, false, "Cosmos Config");
                return null;
            }
        }

        #region PRIVATE METHODS

        /// <summary>
        /// Gets a secret from AWS Secrets Manager
        /// </summary>
        /// <param name="secretName"></param>
        /// <param name="keyId"></param>
        /// <param name="accessKey"></param>
        /// <param name="region"></param>
        /// <returns></returns>
        private async Task<string> GetAwsSecret(string secretName, string keyId, string accessKey, string region)
        {
            //string secretName = "HelloWorld";
            //string region = "us-west-1";

            IAmazonSecretsManager client =
                new AmazonSecretsManagerClient(keyId, accessKey, RegionEndpoint.GetBySystemName(region));

            var request = new GetSecretValueRequest();
            request.SecretId = secretName;
            request.VersionStage = "AWSCURRENT"; // VersionStage defaults to AWSCURRENT if unspecified.

            // In this sample we only handle the specific exceptions for the 'GetSecretValue' API.
            // See https://docs.aws.amazon.com/secretsmanager/latest/apireference/API_GetSecretValue.html
            // We rethrow the exception by default.

            var response = await client.GetSecretValueAsync(request);

            // Decrypts secret using the associated KMS CMK.
            // Depending on whether the secret is a string or binary, one of these fields will be populated.
            if (response.SecretString != null)
            {
                return response.SecretString;
            }

            var reader = new StreamReader(response.SecretBinary);
            return Encoding.UTF8.GetString(Convert.FromBase64String(reader.ReadToEnd()));
        }

        /// <summary>
        ///     Gets the Cosmos Configuration model
        /// </summary>
        /// <param name="configuration">Used when a vault is not being used.</param>
        /// <returns></returns>
        public IOptions<CosmosConfig> GetCosmosConfig(IConfiguration configuration)
        {
            CosmosConfig model;
            if (_cosmosConfigSection == null || string.IsNullOrEmpty(_cosmosConfigSection.Value) || string.IsNullOrWhiteSpace(_cosmosConfigSection.Value))
            {
                //"properties": {
                //    "CosmosAllowConfigEdit": "true",
                //        "CosmosAllowSetup": "true",
                //        "CosmosPrimaryCloud": "azure",
                //        "CosmosSecretName": "CosmosConfig",
                //        "CosmosConfig": "",
                //        "CosmosSendGridApiKey": "[parameters('sendGridApiKey')]",
                //        "CosmosPublisherUrl": "[concat(variables('publisherName'), '.azurewebsites.net')]",
                //        "CosmosEditorUrl": "[concat(variables('editorName'), '.azurewebsites.net')]",
                //        "CosmosStorageUrl": "[reference(concat('Microsoft.Storage/storageAccounts/', variables('storageAccountName')), '2018-07-01').primaryEndpoints.web]",
                //        "DOCKER_REGISTRY_SERVER_PASSWORD": "",
                //        "DOCKER_REGISTRY_SERVER_URL": "https://index.docker.io",
                //        "DOCKER_REGISTRY_SERVER_USERNAME": "",
                //        "WEBSITES_ENABLE_APP_SERVICE_STORAGE": "false"
                //    }

                model = new CosmosConfig();
                model.SecretName = GetValue<string>(configuration, "CosmosSecretName");
                model.MicrosoftAppId = GetValue<string>(configuration, "MicrosoftAppId");
                model.SiteSettings.AllowSetup = GetValue<bool?>(configuration, "CosmosAllowSetup");
                model.PrimaryCloud = GetValue<string>(configuration, "CosmosPrimaryCloud");
                model.SendGridConfig.EmailFrom = GetValue<string>(configuration, "CosmosAdminEmail");
                model.SendGridConfig.SendGridKey = GetValue<string>(configuration, "CosmosSendGridApiKey");
                model.SecretKey = GetValue<string>(configuration, "CosmosSecretKey");
                model.SiteSettings.PublisherUrl = GetValue<string>(configuration, "CosmosPublisherUrl");
                model.SiteSettings.BlobPublicUrl = GetValue<string>(configuration, "CosmosStorageUrl");
                var editorUrl = GetValue<string>(configuration, "CosmosEditorUrl");
                if (!string.IsNullOrEmpty(editorUrl))
                {
                    model.EditorUrls.Add(new EditorUrl() { CloudName = model.PrimaryCloud, Url = editorUrl });
                };

                var dbConnection = configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(dbConnection))
                {
                    dbConnection = GetValue<string>(configuration, "ConnectionStrings_DefaultConnection");
                }
                if (!string.IsNullOrEmpty(dbConnection))
                {
                    var sqlConnectionString = new SqlConnectionString(dbConnection, model.PrimaryCloud, true);
                    model.SqlConnectionStrings.Add(sqlConnectionString);
                }

                if (model.PrimaryCloud.Equals("azure", StringComparison.CurrentCultureIgnoreCase))
                {
                    var blobConnStr = configuration.GetConnectionString("BlobConnection");
                    var container = configuration.GetValue<string>("CosmosBlobContainer");

                    if (!string.IsNullOrEmpty(blobConnStr) && !string.IsNullOrEmpty(container))
                    {
                        model.StorageConfig.AzureConfigs.Add(new Storage.AzureStorageConfig()
                        {
                            AzureBlobStorageConnectionString = blobConnStr,
                            AzureBlobStorageContainerName = container,
                            AzureBlobStorageEndPoint = model.SiteSettings.BlobPublicUrl
                        });
                    }
                }
                else if (model.PrimaryCloud.Equals("amazon", StringComparison.CurrentCultureIgnoreCase))
                {
                    var container = configuration.GetValue<string>("CosmosBlobContainer");
                    var region = configuration.GetValue<string>("AmazonRegion");

                    if (!string.IsNullOrEmpty(_cosmosStartup.AwsKeyId) && !string.IsNullOrEmpty(_cosmosStartup.AwsSecretAccessKey) && !string.IsNullOrEmpty(container) && !string.IsNullOrEmpty(region) && !string.IsNullOrEmpty(model.SiteSettings.BlobPublicUrl))
                    {
                        model.StorageConfig.AmazonConfigs.Add(new Storage.AmazonStorageConfig()
                        {
                            AmazonAwsAccessKeyId = _cosmosStartup.AwsKeyId,
                            AmazonAwsSecretAccessKey = _cosmosStartup.AwsSecretAccessKey,
                            AmazonBucketName = container,
                            AmazonRegion = region,
                            ProfileName = "S3-" + region,
                            ServiceUrl = model.SiteSettings.BlobPublicUrl
                        });
                    }
                }

            }
            else
            {
                model = JsonConvert.DeserializeObject<CosmosConfig>(_cosmosConfigSection.Value);
                model.SecretName = _cosmosStartup.SecretName;
            }

            //
            // Apply boot time overrides
            //
            model.PrimaryCloud = _cosmosStartup.PrimaryCloud;
            model.SiteSettings.AllowReset = _cosmosStartup.AllowSiteReset;
            model.SiteSettings.AllowSetup = _cosmosStartup.AllowSetup;
            model.SiteSettings.AllowConfigEdit = _cosmosStartup.AllowConfigEdit;

            if (model.SqlConnectionStrings.Any())
            {
                foreach (var conn in model.SqlConnectionStrings)
                {
                    conn.IsPrimary = false;
                }
                var primary = model.SqlConnectionStrings.FirstOrDefault(f => f.CloudName.Equals(_cosmosStartup.PrimaryCloud, StringComparison.CurrentCultureIgnoreCase));
                if (primary == null)
                {
                    model.SqlConnectionStrings[0].IsPrimary = true;
                }
                else
                {
                    primary.IsPrimary = true;
                }
            }

            return Options.Create(model);
        }

        private T GetValue<T>(IConfiguration configuration, string keyName)
        {
            object outputValue = null;

            var value = configuration.GetValue<string>(keyName);

            if (string.IsNullOrEmpty(value))
            {
                // Try upper case
                value = configuration.GetValue<string>(keyName.ToUpper());
            }

            if (!string.IsNullOrEmpty(value))
            {
                if (bool.TryParse(value, out var b))
                {
                    outputValue = b;
                }
                else
                {
                    outputValue = value;
                }
            }
            return (T)outputValue;
        }
        #endregion
    }
}