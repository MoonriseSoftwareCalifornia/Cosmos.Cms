using CDT.Cosmos.Cms.Common.Services.Configurations;
using Microsoft.Azure.Management.Cdn;
using Microsoft.Azure.Management.Cdn.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Microsoft.Rest.Azure;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDT.Cosmos.Cms.Services
{
    public enum CdnProvider
    {
        StandardMicrosoft,
        StandardAkamai,
        StandardVerizon,
        PremiumVerizon
    }

    public class CdnManagement
    {
        private readonly string _authority;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _subscriptionId;
        private readonly AzureCdnConfig _config;
        private Profile _profile;
        private Endpoint _endpoint;
        private AuthenticationResult _authenticationResult;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="config"></param>
        public CdnManagement(AzureCdnConfig config)
        {
            _authority = $"https://login.microsoftonline.com/{config.TenantId}/{config.TenantDomainName}";
            _clientId = config.ClientId;
            _clientSecret = config.ClientSecret;
            _subscriptionId = config.SubscriptionId;
            _config = config;

            _profile = GetProfile().Result;
            _endpoint = GetEndPoint().Result;

            try
            {
                CdnProvider = (CdnProvider)Enum.Parse(typeof(CdnProvider), config.CdnProvider);
            }
            catch
            {
                throw new Exception($"CDN provider name {config.CdnProvider} not supported.");
            }
        }

        public CdnProvider CdnProvider { get; }

        /// <summary>
        ///     Authenticates with Azure
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The registered application (find by Client ID) needs to be given "CDN Profile Contributor" permissions.
        ///         <a
        ///             href="https://docs.microsoft.com/en-us/azure/cdn/cdn-app-dev-net#creating-the-azure-ad-application-and-applying-permissions">
        ///             Documentation
        ///         </a>
        ///         suggests assigning this IAM on the resource group.  It can probably be set on the CDN profile instead.
        ///     </para>
        /// </remarks>
        /// <returns></returns>
        public async Task<AuthenticationResult> Authenticate()
        {
            if (_authenticationResult == null)
            {
                var authContext = new AuthenticationContext(_authority);
                var credential = new ClientCredential(_clientId, _clientSecret);
                _authenticationResult = await
                    authContext.AcquireTokenAsync("https://management.core.windows.net/", credential);
            }

            return _authenticationResult;
        }

        /// <summary>
        ///     Gets the CDN management client.
        /// </summary>
        /// <returns></returns>
        public async Task<CdnManagementClient> GetCdnManagementClient()
        {
            var authResult = await Authenticate();

            var cdn = new CdnManagementClient(new TokenCredentials(authResult.AccessToken))
            {
                SubscriptionId = _subscriptionId
            };

            var t = cdn.HttpClient.Timeout;

            return cdn;
        }

        /// <summary>
        ///     Purges one or more CDN paths.
        /// </summary>
        /// <param name="resourceGroupName"></param>
        /// <param name="profileName"></param>
        /// <param name="endpointName"></param>
        /// <param name="contentPaths"></param>
        /// <returns></returns>
        /// <remarks>
        ///     <para>Here are examples of how to set the parameters</para>
        ///     <list type="bullet">
        ///         <item>
        ///             resourceGroupName = "CosmosCMS"
        ///         </item>
        ///         <item>
        ///             profileName = "CosmosCmsCdn"
        ///         </item>
        ///         <item>
        ///             endpointName = Host name not including .azureedge.net
        ///         </item>
        ///         <item>
        ///         </item>
        ///         string [] { "/*" }
        ///     </list>
        ///     <para>
        ///         For more information please see
        ///         <a href="https://docs.microsoft.com/en-us/azure/cdn/cdn-app-dev-net#purge-an-endpoint">getting started</a> with
        ///         the Azure CDN Library for .NET.
        ///     </para>
        /// </remarks>
        public async Task<AzureOperationResponse> PurgeEndpoint(string[] contentPaths)
        {
            using var cdn = await GetCdnManagementClient();
            //await cdn.Endpoints.PurgeContentAsync(_config.ResourceGroup, _config.CdnProfileName, _config.EndPointName, contentPaths.ToList());

            var response = await cdn.Endpoints.PurgeContentWithHttpMessagesAsync(_config.ResourceGroup, _config.CdnProfileName, _config.EndPointName, contentPaths.ToList());
            return response;
        }

        /// <summary>
        /// Get the Azure CDN profile for this connection.
        /// </summary>
        /// <returns></returns>
        public async Task<Profile> GetProfile()
        {
            if (_profile == null)
            {
                using var cdnClient = await this.GetCdnManagementClient();
                try
                {
                    _profile = await cdnClient.Profiles.GetAsync(_config.ResourceGroup, _config.CdnProfileName);
                }
                catch (Exception e)
                {
                    throw new Exception($"Could not connect to Azure CDN Profile {_config.CdnProfileName}. Check permissions.", e);
                }
            }
            return _profile;
        }

        /// <summary>
        /// Gets the end poing for this CDN.
        /// </summary>
        /// <returns></returns>
        public async Task<Endpoint> GetEndPoint()
        {
            using var cdnClient = await GetCdnManagementClient();

            try
            {
                if (_endpoint == null)
                {
                    _endpoint = await cdnClient.Endpoints.GetAsync(_config.ResourceGroup, _config.CdnProfileName, _config.EndPointName);
                }
                return _endpoint;
            }
            catch
            {
                if (_endpoint == null)
                {
                    try
                    {
                        var profileList = cdnClient.Profiles.ListByResourceGroup(_config.ResourceGroup);

                        var allEndpoints = cdnClient.Endpoints.ListByProfile(_config.ResourceGroup, _config.CdnProfileName);
                        if (allEndpoints != null)
                        {
                            foreach(var endpoint in allEndpoints)
                            {
                                var t = endpoint.Name;
                            }
                            var list = allEndpoints.Select(s => s.Name).ToArray();
                            var names = string.Join(",", list);
                            throw new Exception($"Profile '{_config.CdnProfileName}' does not contain endpoint name '{_config.EndPointName}.' Did you mean to use one of these endpoints: {names}?");
                        }
                        allEndpoints.Select(s => s.Name).ToArray();
                        throw new Exception($"Could not connect to Azure CDN Endpoint for profile {_config.CdnProfileName} and end point {_config.EndPointName}. Check permissions.");
                    }
                    catch (Exception e2)
                    {
                        var t = e2; // For debugging purposes.
                    }
                }
                throw;
            }
        }
    }
}