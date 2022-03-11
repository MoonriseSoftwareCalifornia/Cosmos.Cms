using CDT.Cosmos.Cms.Services.Secrets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace Cosmos.Tests
{
    [TestClass]
    public class CORE_G03_SecretClient_Tests
    {
        private static Guid _testValue;
        private static AzureKeyVaultClient? _azureClient;
        private static AwsSecretsManagerClient? _awsSecretsManagerClient;
        private static SecretsClient? _secretsClient;

        private const string SECRETNAME = "CosmosUnitTestMySecret";

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            var utils = new Utilities();
            var config = utils.GetCosmosBootConfig();
            _testValue = Guid.NewGuid();

            TestValue("CosmosAzureVaultUrl", config.CosmosAzureVaultUrl);
            TestValue("AzureVaultTenantId", config.AzureVaultTenantId);
            TestValue("AzureVaultClientId", config.AzureVaultClientId);
            TestValue("AzureVaultClientSecret", config.AzureVaultClientSecret);
            TestValue("AwsKeyId", config.AwsKeyId);
            TestValue("AwsSecretAccessKey", config.AwsSecretAccessKey);
            TestValue("AwsSecretsRegion", config.AwsSecretsRegion);

            _azureClient = new AzureKeyVaultClient(config.CosmosAzureVaultUrl,
                config.AzureVaultTenantId,
                config.AzureVaultClientId,
                config.AzureVaultClientSecret);

            _awsSecretsManagerClient = new AwsSecretsManagerClient(config.AwsKeyId,
                config.AwsSecretAccessKey,
                config.AwsSecretsRegion);

            _secretsClient = new SecretsClient(config);
        }

        private static void TestValue(string variableName, string value)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value))
            {
                throw new Exception($"Variable { variableName } is not a valid value");
            }
        }

        [TestMethod]
        public async Task A00_Azure_SetAndGetSecret()
        {
            await _azureClient.SetSecret(SECRETNAME, _testValue.ToString());

            var result = await _azureClient.GetSecret(SECRETNAME);

            Assert.AreEqual(_testValue.ToString(), result);
        }

        [TestMethod]
        public async Task A01_Azure_GetSecret_NotFoundTest()
        {
            var result = await _azureClient.GetSecret(Guid.NewGuid().ToString());

            Assert.AreEqual("404 (Not found)", result);
        }

        [TestMethod]
        public async Task A02_Amazon_SetAndGetSecret()
        {
            await _awsSecretsManagerClient.SetSecret(SECRETNAME, _testValue.ToString());

            var result = await _awsSecretsManagerClient.GetSecret(SECRETNAME);

            Assert.AreEqual(_testValue.ToString(), result);
        }

        [TestMethod]
        public async Task A03_Amazon_GetSecret_NotFoundTest()
        {
            var result = await _awsSecretsManagerClient.GetSecret(Guid.NewGuid().ToString());

            Assert.AreEqual("404 (Not found)", result);
        }

        [TestMethod]
        public async Task A04_SecretsClient_WriteTest()
        {
            var testValue = Guid.NewGuid().ToString();

            await _secretsClient.SetSecret(SECRETNAME, testValue);

            var result1 = await _secretsClient.GetSecret(SECRETNAME);
            var result2 = await _azureClient.GetSecret(SECRETNAME);
            var result3 = await _awsSecretsManagerClient.GetSecret(SECRETNAME);

            Assert.AreEqual(testValue, result1);
            Assert.AreEqual(testValue, result2);
            Assert.AreEqual(testValue, result3);
        }

        [TestMethod]
        public async Task A06_SecretsClient_NotConfiguredTest()
        {
            var testValue = Guid.NewGuid().ToString();

            var secretsClient = new SecretsClient(new CDT.Cosmos.Cms.Common.Services.Configurations.CosmosStartup() { AllowConfigEdit = false });

            Assert.IsFalse(secretsClient.IsConfigured);

            await Assert.ThrowsExceptionAsync<Exception>(async ()=> await secretsClient.GetSecret(SECRETNAME));
        }
    }
}
