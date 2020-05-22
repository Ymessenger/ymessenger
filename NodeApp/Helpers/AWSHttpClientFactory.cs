/** 
  *    This file is part of Y messenger.
  *
  *    Y messenger is free software: you can redistribute it and/or modify
  *    it under the terms of the GNU Affero Public License as published by
  *    the Free Software Foundation, either version 3 of the License, or
  *    (at your option) any later version.
  *
  *    Y messenger is distributed in the hope that it will be useful,
  *    but WITHOUT ANY WARRANTY; without even the implied warranty of
  *    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  *    GNU Affero Public License for more details.
  *
  *    You should have received a copy of the GNU Affero Public License
  *    along with Y messenger.  If not, see <https://www.gnu.org/licenses/>.
  */
using Amazon.Runtime;
using System.Net;
using System.Net.Http;

namespace NodeApp.Helpers
{
    public class AWSHttpClientFactory : Amazon.Runtime.HttpClientFactory
    {
        public override HttpClient CreateHttpClient(IClientConfig clientConfig)
        {
            var httpMessageHandler = CreateClientHandler();
            if (clientConfig.MaxConnectionsPerServer.HasValue)
            {
                httpMessageHandler.MaxConnectionsPerServer = clientConfig.MaxConnectionsPerServer.Value;
            }
            httpMessageHandler.AllowAutoRedirect = clientConfig.AllowAutoRedirect;
            httpMessageHandler.AutomaticDecompression = DecompressionMethods.None;
            var proxy = clientConfig.GetWebProxy();
            if (proxy != null)
            {
                httpMessageHandler.Proxy = proxy;
            }
            if (httpMessageHandler.Proxy != null && clientConfig.ProxyCredentials != null)
            {
                httpMessageHandler.Proxy.Credentials = clientConfig.ProxyCredentials;
            }
            var httpClient = new HttpClient(httpMessageHandler);
            if (clientConfig.Timeout.HasValue)
            {
                httpClient.Timeout = clientConfig.Timeout.Value;
            }
            return httpClient;
        }
        protected virtual HttpClientHandler CreateClientHandler() =>
            new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; }
            };
    }
}
