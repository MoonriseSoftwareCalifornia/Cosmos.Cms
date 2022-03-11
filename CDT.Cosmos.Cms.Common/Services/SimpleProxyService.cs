using CDT.Cosmos.Cms.Common.Services.Configurations;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace CDT.Cosmos.Cms.Common.Services
{
    /// <summary>
    ///     This is a generic class that makes server to server calls
    /// </summary>
    public class SimpleProxyService
    {
        /// <summary>
        ///     Optsions field
        /// </summary>
        private readonly IOptions<SimpleProxyConfigs> config;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="config"></param>
        public SimpleProxyService(IOptions<SimpleProxyConfigs> config)
        {
            this.config = config;
        }

        /// <summary>
        ///     Calls and endpoint and returns the results as a string.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="user"></param>
        /// <param name="proxyData"></param>
        /// <returns></returns>
        public async Task<string> CallEndpoint(string name, UserIdentityInfo user, string proxyData = "")
        {
            var endpointConfig =
                config.Value.Configs.FirstOrDefault(c =>
                    c.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));

            if (endpointConfig == null)
                throw new Exception("Endpoint not found.");

            if (string.IsNullOrEmpty(endpointConfig.Data)) endpointConfig.Data = proxyData;

            if (endpointConfig.Roles.Contains("Anonymous")
                ||
                user != null && endpointConfig.Roles.Contains("Authenticated") && user.IsAuthenticated
                ||
                user != null && endpointConfig.Roles.Any(a => user.IsInRole(a))
            )
                return await CallEndpoint(
                    new Uri(endpointConfig.UriEndpoint),
                    endpointConfig.Method,
                    endpointConfig.Data,
                    endpointConfig.UserName,
                    endpointConfig.Password,
                    endpointConfig.ContentType);

            return "Permission denied.";
        }

        /// <summary>
        ///     Use this private method to call and end point
        /// </summary>
        /// <returns></returns>
        private async Task<string> CallEndpoint(Uri endPoint, string method, string proxyData, string userName,
            string password, string contentType = "application/x-www-form-urlencoded")
        {
            HttpClient httpClient;

            if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(password))
            {
                httpClient = new HttpClient(new HttpClientHandler()
                {
                      PreAuthenticate = true,
                    UseDefaultCredentials = false,
                    UseProxy = false,
                    Credentials = new NetworkCredential(userName, password)
                });
            } 
            else
            {
                httpClient = new HttpClient();
            }
            
            using (httpClient)
            {
                HttpResponseMessage response;

                // WebRequest request;
                if (method.Equals("get", StringComparison.CurrentCultureIgnoreCase))
                {
                    response = await httpClient.GetAsync(endPoint + proxyData);
                }
                else
                {
                    HttpContent httpContent = new StringContent(proxyData, Encoding.UTF8);
                    httpContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

                    response = await httpClient.PostAsync(endPoint, httpContent);
                }

                return await response.Content.ReadAsStringAsync();
            }

        }
    }
}