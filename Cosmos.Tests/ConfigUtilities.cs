using CDT.Cosmos.Cms.Common.Services.Configurations;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Reflection;

namespace Cosmos.Tests
{
    public class ConfigUtilities
    {
        private static CosmosStartup? _cosmosBootConfig;

        private static string GetKeyValue(IConfigurationRoot config, string key)
        {
            var data = config[key];
            if (string.IsNullOrEmpty(data))
            {
                data = Environment.GetEnvironmentVariable(key);
                if (string.IsNullOrEmpty(data))
                {
                    data = Environment.GetEnvironmentVariable(key.ToUpper());
                }
            }
            return data;
        }

        internal static IOptions<CosmosConfig> GetCosmosConfig()
        {
            // the type specified here is just so the secrets library can 
            // find the UserSecretId we added in the csproj file
            //var jsonConfig = Path.Combine(Environment.CurrentDirectory, "appsettings.json");

            var builder = new ConfigurationBuilder()
                //.AddJsonFile(jsonConfig, true)
                .AddUserSecrets(Assembly.GetExecutingAssembly(), true); // User secrets override all - put here

            var config = Retry.Do(() => builder.Build(), TimeSpan.FromSeconds(1));

            // From either local secrets or app config, get connection info for Azure Vault.
            var tenantId = GetKeyValue(config, "CosmosAzureVaultTenantId");

            var clientId = GetKeyValue(config, "CosmosAzureVaultClientId");

            var key = GetKeyValue(config, "CosmosAzureVaultClientSecret");

            var vaultUrl = GetKeyValue(config, "CosmosAzureVaultUrl");

            var secretName = GetKeyValue(config, "CosmosSecretName");

            _cosmosBootConfig = new CosmosStartup
            {
                AllowConfigEdit = true,
                AzureVaultClientId = clientId,
                AzureVaultClientSecret = key,
                AzureVaultTenantId = tenantId,
                CosmosAzureVaultUrl = vaultUrl,
                SecretName = secretName,
                UseAzureVault = true,
                PrimaryCloud = "azure"
            };

            var startupConfig = new CosmosOptionsBuilder(_cosmosBootConfig);

            var cosmosConfig = startupConfig.Build(config);
            return cosmosConfig;
        }

        public static IMemoryCache GetMemoryCache()
        {
            var options = Options.Create(new MemoryCacheOptions()
            {
                SizeLimit = 20000000 // 20 megabytes decimal
            });
            return new MemoryCache(options);
        }
    }
}