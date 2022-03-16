using CDT.Cosmos.Cms.Common.Services.Configurations;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CDT.Cosmos.Cms.Services.Secrets
{
    /// <summary>
    /// Secrets management client.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This service creates, updates and reads secrets from
    /// either Amazon Secrets Manager and Azure Key Vault, or both
    /// at the same time.
    /// </para>
    /// <para>
    /// To enable it, the environment variable CosmosAllowConfigEdit must be set to true.
    /// </para>
    /// <para>IMPORTANT! Do not set CosmosAllowConfigEdit to true when in production.</para>
    /// </remarks>
    public class SecretsClient : ISecretsClient
    {
        private readonly CosmosStartup _config;
        private readonly List<ISecretsClient> _secretsClients;
        private readonly string _primaryCloud;
        private readonly bool _allowConfigEdit;

        /// <summary>
        /// Default secret name as defined in <see cref="CosmosStartup.SecretName"/>
        /// </summary>
        /// <remarks>This property is disabled when <see cref="CosmosStartup.AllowConfigEdit"/> is false.</remarks>
        public string DefaultSecretName
        {
            get
            {
                return _config.SecretName;
            }
        }

        /// <summary>
        /// Indicates if the secrets client is configured
        /// </summary>
        /// <remarks>
        /// Returns true when <see cref="CosmosStartup.AllowConfigEdit"/> is set to true and there is at least one <see cref="ISecretsClient"/> defined.
        /// </remarks>
        public bool IsConfigured
        {
            get
            {
                return _allowConfigEdit &&
                    ((!string.IsNullOrEmpty(_config.CosmosAzureVaultUrl) && !string.IsNullOrEmpty(_config.AzureVaultTenantId) && !string.IsNullOrEmpty(_config.AzureVaultClientId) && !string.IsNullOrEmpty(_config.AzureVaultClientSecret)) || 
                    (!string.IsNullOrEmpty(_config.AwsKeyId) && !string.IsNullOrEmpty(_config.AwsSecretAccessKey) && !string.IsNullOrEmpty(_config.AwsSecretsRegion)));
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="config"></param>
        public SecretsClient(CosmosStartup config)
        {
            _config = config;
            _secretsClients = new List<ISecretsClient>();
            _primaryCloud = config.PrimaryCloud;
            _allowConfigEdit = config.AllowConfigEdit;

        }

        private void CreateCloudClients()
        {
            if (_config.AllowConfigEdit == true)
            {
                if (_config == null)
                {
                    throw new System.Exception("Cosmos startup is null");
                }
                if (!string.IsNullOrEmpty(_config.CosmosAzureVaultUrl) && !string.IsNullOrEmpty(_config.AzureVaultTenantId) && !string.IsNullOrEmpty(_config.AzureVaultClientId) && !string.IsNullOrEmpty(_config.AzureVaultClientSecret))
                {
                    _secretsClients.Add(new AzureKeyVaultClient(_config.CosmosAzureVaultUrl,
                    _config.AzureVaultTenantId,
                    _config.AzureVaultClientId,
                    _config.AzureVaultClientSecret));
                }

                if (!string.IsNullOrEmpty(_config.AwsKeyId) && !string.IsNullOrEmpty(_config.AwsSecretAccessKey) && !string.IsNullOrEmpty(_config.AwsSecretsRegion))
                {
                    _secretsClients.Add(new AwsSecretsManagerClient(_config.AwsKeyId,
                        _config.AwsSecretAccessKey,
                        _config.AwsSecretsRegion));
                }
            }
        }

        private void Dispose()
        {
            foreach (var client in _secretsClients)
            {
                if (client.GetType() == typeof(AwsSecretsManagerClient))
                {
                    var c = (AwsSecretsManagerClient)client;
                    c.Dispose();
                }
            }
            _secretsClients.Clear();
        }

        /// <summary>
        /// Gets the secret from the default secret name (i.e. <see cref="DefaultSecretName"/>).
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetDefaultSecret()
        {
            return await GetSecret(DefaultSecretName);
        }

        /// <summary>
        /// Get secret
        /// </summary>
        /// <param name="secreteName"></param>
        /// <returns></returns>
        public async Task<string> GetSecret(string secreteName)
        {
            CreateCloudClients();

            if (!IsConfigured)
            {
                throw new System.Exception("Permission denied.");
            }

            ISecretsClient client = null;

            if (_secretsClients.Count == 1)
            {
                client = _secretsClients.FirstOrDefault();
            }
            else
            {
                if (_primaryCloud == "azure")
                {
                    client = _secretsClients.FirstOrDefault(c => c.GetType() == typeof(AzureKeyVaultClient));
                    return await client.GetSecret(secreteName);
                }
                else if (_primaryCloud == "amazon")
                {
                    client = _secretsClients.FirstOrDefault(c => c.GetType() == typeof(AwsSecretsManagerClient));
                }
            }

            if (client == null)
            {
                return "";
            }

            var secret = await client.GetSecret(secreteName);
            Dispose();

            return secret;
        }

        /// <summary>
        /// Create or update a secret
        /// </summary>
        /// <param name="secreteName"></param>
        /// <param name="secret"></param>
        /// <returns></returns>
        public async Task SetSecret(string secreteName, string secret)
        {
            CreateCloudClients();

            if (!IsConfigured)
            {
                throw new System.Exception("Permission denied.");
            }

            foreach (var client in _secretsClients)
            {
                await client.SetSecret(secreteName, secret);
            }

            Dispose();
        }
    }
}
