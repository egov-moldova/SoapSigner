using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Client.Settings;
using Microsoft.Extensions.Options;
using System.Xml;

namespace Client.Clients
{
    public abstract class BaseClient : IEndpointClient
    {
        private readonly IHttpClientFactory clientFactory;
        protected ClientCertificateOptions Options;

        protected BaseClient(IHttpClientFactory clientFactory, IOptionsMonitor<ClientCertificateOptions> optionsAccessor)
        {
            this.clientFactory = clientFactory;
            Options = optionsAccessor.CurrentValue;
            optionsAccessor.OnChange((options, name) => Options = options);
        }

        public abstract HttpRequestMessage BuildRequest(RequestSettings requestSettings);

        public async Task<HttpResponseMessage> SendRequest(HttpRequestMessage request, long timeout = 0)
        {
            var client = clientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMilliseconds(timeout);
            return await client.SendAsync(request);
        }

        public async Task<JToken> ProcessResponse(ResponseSettings responseSettings, HttpResponseMessage response)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            return ProcessResponseBody(responseSettings, responseBody);
        }

        public abstract JToken ProcessResponseBody(ResponseSettings responseSettings, string responseBody);

        public abstract XmlNode GetResponseBody(ResponseSettings responseSettings, string responseBody);

        protected static void FillHeaders(string rawHeaders, HttpHeaders headers)
        {
            if (string.IsNullOrWhiteSpace(rawHeaders)) return;

            foreach (var rawHeader in rawHeaders.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
            {
                var headerText = Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(rawHeader)); // HTTP headers can have only ASCII chars
                var indexOfColon = headerText.IndexOf(':');
                if (indexOfColon <= 0) continue;
                headers.TryAddWithoutValidation(headerText.Substring(0, indexOfColon).Trim(), headerText.Substring(indexOfColon + 1).Trim());
            }
        }
    }
}
