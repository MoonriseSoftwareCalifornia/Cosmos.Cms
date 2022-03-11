using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CDT.Cosmos.Cms.Services.Secrets
{
    /// <summary>
    /// AWS Secrets Manager Client
    /// </summary>
    public class AwsSecretsManagerClient : ISecretsClient, IDisposable
    {
        private AmazonSecretsManagerClient? _client;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="accessKeyId"></param>
        /// <param name="secretAccessKey"></param>
        /// <param name="awsRegion"></param>
        public AwsSecretsManagerClient(string accessKeyId, string secretAccessKey, string awsRegion)
        {
            var regionIdentifier = RegionEndpoint.GetBySystemName(awsRegion);
            _client = new AmazonSecretsManagerClient(accessKeyId, secretAccessKey, regionIdentifier);
        }

        /// <summary>
        /// Dispose of this service when done.
        /// </summary>
        public void Dispose()
        {
            if (_client != null)
            {
                _client.Dispose();
                _client = null;
            }
        }

        /// <summary>
        /// Gets a secret
        /// </summary>
        /// <param name="secretname"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<string> GetSecret(string secretname)
        {
            try
            {
                var result = await _client.GetSecretValueAsync(new GetSecretValueRequest()
                {
                    SecretId = secretname
                });
                return result.SecretString;
            }
            catch (ResourceNotFoundException)
            {
                return "404 (Not found)";
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Sets a secret.
        /// </summary>
        /// <param name="secreteName"></param>
        /// <param name="secret"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task SetSecret(string secreteName, string secret)
        {
            var request = new ListSecretsRequest();

            var result = await _client.ListSecretsAsync(request);

            //var item = 

            if (result.SecretList.Any(w => w.Name.ToLower() == secreteName.ToLower()))
            {
                await _client.PutSecretValueAsync(new PutSecretValueRequest()
                {
                    SecretId = secreteName,
                    SecretString = secret
                });

            }
            else
            {
                await _client.CreateSecretAsync(
                       new CreateSecretRequest()
                       {
                           Name = secreteName,
                           SecretString = secret
                       });
            }
        }
    }
}
