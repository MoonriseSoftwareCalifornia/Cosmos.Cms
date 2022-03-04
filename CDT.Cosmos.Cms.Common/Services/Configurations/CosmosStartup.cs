using CDT.Cosmos.Cms.Common.Services.Configurations.BootUp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CDT.Cosmos.Cms.Common.Services.Configurations
{
    /// <summary>
    ///     Cosmos startup configuration
    /// </summary>
    public class CosmosStartup
    {
        #region CONSTRUCTORS

        /// <summary>
        /// Parameterless constructor. No validation.
        /// </summary>
        public CosmosStartup()
        {
        }

        /// <summary>
        /// Builds boot configuration and performs validation.
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="services"></param>
        /// <remarks>
        /// Validates the boot configuration.
        /// </remarks>
        public CosmosStartup(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        #endregion

        private readonly IConfiguration _configuration;

        #region PRIVATE METHODS

        /// <summary>
        /// Adds a list of diagnostics.
        /// </summary>
        /// <param name="diagnostic"></param>
        private void AddDiagnostics(IEnumerable<Diagnostic> diagnostics)
        {
            Diagnostics.AddRange(diagnostics);
        }

        /// <summary>
        /// Adds as an error log message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="isOk">Is OK or informational, or not an error causing Cosmos not to run</param>
        /// <param name="serviceType"></param>
        private void AddDiagnostic(string message, bool isOk, string serviceType = "Boot Configuration")
        {
            Diagnostics.Add(new Diagnostic()
            {
                Message = message,
                Success = isOk,
                ServiceType = serviceType
            });
        }

        /// <summary>
        /// Gets a value from the configuration, and records what was found.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="valueName"></param>
        /// <returns></returns>
        private T GetValue<T>(string valueName)
        {
            var val = _configuration[valueName];

            object outputValue;

            AddDiagnostic($"INFORMATIONAL: Read boot variable {valueName} with value: '{val}'.", true);

            if (typeof(T) == typeof(bool))
            {
                // default to false
                if (string.IsNullOrEmpty(val))
                {
                    AddDiagnostic($"INFORMATIONAL: Boot variable {valueName} set to false.", true);
                    outputValue = false;
                }
                else if (bool.TryParse(val, out bool parsedValue))
                {
                    AddDiagnostic($"INFORMATIONAL: Boot variable {valueName} successfully parsed as {parsedValue}.", true);
                    outputValue = parsedValue;
                }
                else
                {
                    AddDiagnostic($"INFORMATIONAL: Could not parse variable {valueName} as bool using value {val}.", true);
                    AddDiagnostic($"INFORMATIONAL: Boot variable {valueName} set to {false}.", true);
                    outputValue = false;
                }
            }
            else if (typeof(T) == typeof(bool?))
            {
                if (bool.TryParse(val, out bool parsedValue))
                {
                    AddDiagnostic($"INFORMATIONAL: Boot variable {valueName} successfully parsed as {parsedValue}.", true);
                    outputValue = parsedValue;
                }
                else
                {
                    AddDiagnostic($"INFORMATIONAL: Could not parse variable {valueName} as bool using value {val}.", true);
                    AddDiagnostic($"INFORMATIONAL: Boot variable {valueName} set to {false}.", true);
                    outputValue = null;
                }
            }
            else if (typeof(T) == typeof(Uri))
            {
                if (Uri.TryCreate(val, UriKind.Absolute, out Uri uri))
                {
                    AddDiagnostic($"INFORMATIONAL: Boot variable {valueName} successfully parsed as {uri.ToString()}.", true);
                    outputValue = uri;
                }
                else
                {
                    AddDiagnostic($"INFORMATIONAL: Could not parse variable {valueName} as Uri using value {val}.", true);
                    AddDiagnostic($"INFORMATIONAL: Boot variable {valueName} set to {null}.", true);
                    outputValue = null;
                }
            }
            else
            {
                outputValue = val;
            }

            return (T)outputValue;
        }

        private string GetConnectionString(string name, string primaryCloud)
        {
            string connectionString = "";
            if (primaryCloud == "azure")
            {
                connectionString = _configuration.GetConnectionString(name);
            }
            else if (primaryCloud == "amazon")
            {
                var variableName = "ConnectionStrings_DefaultConnection";
                connectionString = _configuration[variableName];
                if (string.IsNullOrEmpty(connectionString))
                {
                    connectionString = _configuration[variableName.ToUpper()];
                }
            }

            if (string.IsNullOrEmpty(connectionString))
            {
                AddDiagnostic($"INFORMATIONAL: Connection string {name} is null or empty.", true);
            }
            else
            {
                AddDiagnostic($"INFORMATIONAL: Connection string {name} was found.", true);
            }

            return connectionString;
        }

        #region BOOT TIME CONFIGURATION BUILDING AND VALIDATION METHODS

        /// <summary>
        /// Reads boot time configuration and performs validation
        /// </summary>
        private void ReadBootConfig()
        {

            #region PARSE OUT VALUES

            // REQUIRED VALUES
            PrimaryCloud = GetValue<string>("CosmosPrimaryCloud");
            if (!string.IsNullOrEmpty(PrimaryCloud))
            {
                PrimaryCloud = PrimaryCloud.ToLower();
            }

            SecretName = GetValue<string>("CosmosSecretName");

            // SETUP VALUES
            AllowConfigEdit = GetValue<bool>("CosmosAllowConfigEdit");
            AllowSetup = GetValue<bool>("CosmosAllowSetup");
            AllowSiteReset = GetValue<bool>("CosmosAllowSiteReset");

            // AZURE VAULT VALUES
            UseAzureVault = GetValue<bool>("CosmosUseAzureVault");
            UseDefaultCredential = GetValue<bool>("CosmosUseDefaultCredential");
            AzureVaultClientId = GetValue<string>("CosmosAzureVaultClientId");
            AzureVaultClientSecret = GetValue<string>("CosmosAzureVaultClientSecret");
            AzureVaultTenantId = GetValue<string>("CosmosAzureVaultTenantId");
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
            var vaultUri = GetValue<Uri?>("CosmosAzureVaultUrl");
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
            if (vaultUri != null)
                CosmosAzureVaultUrl = vaultUri.ToString();

            // AWS SECRETS MANAGER VALUES
            UseAwsSecretsMgr = GetValue<bool>("CosmosUseAwsSecretsMgr");
            AwsSecretsRegion = GetValue<string>("CosmosAwsSecretsRegion");
            AwsKeyId = GetValue<string>("CosmosAwsKeyId");
            AwsSecretAccessKey = GetValue<string>("CosmosAwsSecretAccessKey");

            var dbConnection = GetConnectionString("DefaultConnection", PrimaryCloud);

            #endregion

            #region VALIDATE REQUIRED VALUES

            if (string.IsNullOrEmpty(SecretName) && string.IsNullOrEmpty(dbConnection))
            {
                AddDiagnostic("Boot environment variable CosmosSecretName is null or empty.", false);
            }
            else
            {
                AddDiagnostic($"Secret Name is set to '{SecretName}' or DefaultConnection and BlobConnection provided.", true);
            }

            if (string.IsNullOrEmpty(PrimaryCloud))
            {
                AddDiagnostic("Boot environment variable CosmosPrimaryCloud is null or empty.", false);
            }
            else
            {

                if (PrimaryCloud != "azure" && PrimaryCloud != "amazon")
                {
                    AddDiagnostic($"CosmosPrimaryCloud must be set to azure or amazon, not '{PrimaryCloud}'.", false);
                }
                else
                {
                    AddDiagnostic($"Primary cloud is set to '{PrimaryCloud}'.", true);
                }
            }

            #endregion

            #region VALIDATE SECRETS SETTINGS (AZURE VAULT/SECRETS MANAGER/LOCAL SECRETS)

            if (UseAzureVault)
            {
                AddDiagnostic($"Using Azure Vault: '{CosmosAzureVaultUrl}'.", true);

                if (string.IsNullOrEmpty(CosmosAzureVaultUrl))
                {
                    AddDiagnostic("Azure Vault Url is missing or not configured. Make sure CosmosAzureVaultUrl has a valid URL to the Key Vault.", false);
                }
                else
                {
                    if (UseDefaultCredential)
                    {
                        AddDiagnostic("Using the Default Identity to connect to azure Vault.", true);
                    }
                    else
                    {
                        AddDiagnostic("Using authentication to connect to azure Vault.", true);

                        if (string.IsNullOrEmpty(AzureVaultClientId))
                        {
                            AddDiagnostic("Azure Vault client ID (CosmosAzureVaultClientId) is null or empty.", false);
                        }

                        if (string.IsNullOrEmpty(AzureVaultClientSecret))
                        {
                            AddDiagnostic("Azure Vault client secret (AzureVaultClientSecret) is null or empty.", false);
                        }

                        if (string.IsNullOrEmpty(AzureVaultTenantId))
                        {
                            AddDiagnostic("Azure Vault client tenant ID (AzureVaultTenantId) is null or empty.", false);
                        }
                    }
                }
            }
            else if (UseAwsSecretsMgr)
            {
                AddDiagnostic("Using AWS Secrets Manager.", true);

                if (string.IsNullOrEmpty(AwsSecretsRegion))
                {
                    AddDiagnostic("AWS Secrets Manager region (CosmosAwsSecretsRegion) is null or empty.", false);
                }

                if (string.IsNullOrEmpty(AwsKeyId))
                {
                    AddDiagnostic("AWS Key Id (CosmosAwsKeyId) is null or empty.", false);
                }

                if (string.IsNullOrEmpty(AwsSecretAccessKey))
                {
                    AddDiagnostic("AWS Secret Access Key (CosmosAwsSecretAccessKey) is null or empty.", false);
                }
            }

            #endregion
        }

        #endregion

        #endregion

        /// <summary>
        /// Returns the configuration status of Cosmos.
        /// </summary>
        /// <returns></returns>
        public CosmosConfigStatus GetStatus()
        {
            return new CosmosConfigStatus()
            {
                DateTimeStamp = DateTimeOffset.UtcNow,
                Diagnostics = Diagnostics,
                ReadyToRun = HasErrors == false
            };
        }

        /// <summary>
        /// Attempts to run Cosmos Startup.
        /// </summary>
        /// <remarks>
        /// <para>This method tries to run Cosmos and collects diagnostics in the process.</para>
        /// <para>Check <see cref="HasErrors"/> to see if there are any errors detected.</para>
        /// <para>
        /// If boot time value CosmosAllowSetup is set to true, then diagnostic tests are 
        /// run to determine if cloud resource can be connected to. This can significantly delay boot
        /// up time for Cosmos.  Once Cosmos is setup, set CosmosAllowSetup to false.
        /// </para>
        /// </remarks>
        public bool TryRun(out IOptions<CosmosConfig> config)
        {
            config = null;

            // Read the boot configuration values and check them
            ReadBootConfig();

            if (!HasErrors)
            {
                // Step one, read the boot time configuration variables and validate.
                var cosmosOptionsBuilder = new CosmosOptionsBuilder(this);

                // If no errors, then continue.
                if (cosmosOptionsBuilder.HasErrors)
                {
                    AddDiagnostics(cosmosOptionsBuilder.Diagnostics);
                }
                else
                {
                    // Now try to build the options
                    config = cosmosOptionsBuilder.Build(_configuration);
                    AddDiagnostics(cosmosOptionsBuilder.Diagnostics);

                    // Check for errors trying to build the options and
                    // if we are in setup mode, run diagnostics
                    if (!cosmosOptionsBuilder.HasErrors && AllowSetup)
                    {
                        var diagnosticTests = new DiagnosticTests(config);
                        var results = diagnosticTests.Run().Result;
                        AddDiagnostics(results);
                    }
                }

            }
            else
            {
                if (config == null)
                {
                    config = Options.Create(new CosmosConfig());
                }
            }
                        

            // If there are no errors, we have success.
            return HasErrors == false;
        }

        #region PUBLIC PROPERTIES

        /// <summary>
        /// Diagnostics results
        /// </summary>
        public readonly List<Diagnostic> Diagnostics = new();

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

        #region REQUIRED REGIONS

        /// <summary>
        ///     Identifies what cloud we are in
        /// </summary>
        public string PrimaryCloud { get; set; } = "";

        /// <summary>
        ///     Cosmos Azure Vault Secret Name
        /// </summary>
        public string SecretName { get; set; } = "";

        #endregion

        #region VALUES USED DURING SETUP

        /// <summary>
        ///     Allows the configuration editor to make secrets changes
        /// </summary>
        public bool AllowConfigEdit { get; set; } = false;

        /// <summary>
        ///     Allows the site to be setup
        /// </summary>
        public bool AllowSetup { get; set; } = false;

        /// <summary>
        ///     Allows the site to be wiped clean (factor reset)
        /// </summary>
        public bool AllowSiteReset { get; set; } = false;

        #endregion

        #region AZURE VAULT SETTINGS

        /// <summary>
        ///     Client ID (aka App Id)
        /// </summary>
        public string AzureVaultClientId { get; set; } = "";

        /// <summary>
        ///     Client secret
        /// </summary>
        public string AzureVaultClientSecret { get; set; } = "";

        /// <summary>
        ///     Azure Tenant Id
        /// </summary>
        public string AzureVaultTenantId { get; set; } = "";

        /// <summary>
        ///     Key vault URL
        /// </summary>
        public string CosmosAzureVaultUrl { get; set; } = "";

        /// <summary>
        ///     Use Azure Vault
        /// </summary>
        public bool UseAzureVault { get; set; } = false;

        /// <summary>
        ///     Use Azure Web App Default Credential
        /// </summary>
        public bool UseDefaultCredential { get; set; } = false;

        #endregion

        #region AWS Secrets Manager

        /// <summary>
        ///     Use AWS Secrets Manager?
        /// </summary>
        public bool UseAwsSecretsMgr { get; set; } = false;

        /// <summary>
        ///     Region where AWS secrets manager is located
        /// </summary>
        public string AwsSecretsRegion { get; set; }

        /// <summary>
        ///     AWS Key Id
        /// </summary>
        public string AwsKeyId { get; set; }

        /// <summary>
        ///     AWS Secret Access Key
        /// </summary>
        public string AwsSecretAccessKey { get; set; }

        #endregion

        #endregion
    }
}