using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using System;
using System.Threading.Tasks;

namespace CDT.Cosmos.Cms.Services.Secrets
{
    /// <summary>
    /// Azure key vault client
    /// </summary>
    public class AzureKeyVaultClient : ISecretsClient
    {
        private readonly SecretClient _client;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="keyVaultUrl"></param>
        /// <param name="tenantId"></param>
        /// <param name="clientId"></param>
        /// <param name="clientSecret"></param>
        public AzureKeyVaultClient(string keyVaultUrl, string tenantId, string clientId, string clientSecret)
        {
            _client = new SecretClient(new Uri(keyVaultUrl), new ClientSecretCredential(tenantId, clientId, clientSecret));
        }

        /// <summary>
        /// Set Keyvault secret
        /// </summary>
        /// <param name="secreteName"></param>
        /// <param name="secret"></param>
        /// <returns></returns>
        public async Task SetSecret(string secreteName, string secret)
        {
            _ = await _client.SetSecretAsync(new KeyVaultSecret(secreteName, secret));
        }

        /// <summary>
        /// Gets a Keyvault secret
        /// </summary>
        /// <param name="secretname"></param>
        /// <returns></returns>
        public async Task<string> GetSecret(string secretname)
        {
            try
            {
                var result = await _client.GetSecretAsync(secretname);

                return result.Value.Value;
            }
            catch (Azure.RequestFailedException rfe)
            {
                if (rfe.Status == 404)
                {
                    return "404 (Not found)";
                }
                throw rfe;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

    }
}
