using System.Threading.Tasks;

namespace CDT.Cosmos.Cms.Services.Secrets
{
    /// <summary>
    /// Amazon or Azure Secrets Client
    /// </summary>
    public interface ISecretsClient
    {
        /// <summary>
        /// Sets a secret
        /// </summary>
        /// <param name="secreteName"></param>
        /// <param name="secret"></param>
        /// <returns></returns>
        Task SetSecret(string secreteName, string secret);

        /// <summary>
        /// Gets a secret
        /// </summary>
        /// <param name="secretname"></param>
        /// <returns></returns>
        Task<string> GetSecret(string secretname);
    }
}
