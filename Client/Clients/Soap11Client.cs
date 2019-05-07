using Client.Settings;
using Microsoft.Extensions.Options;
using System.Net.Http;

namespace Client.Clients
{
    public sealed class Soap11Client : SoapClient
    {
        public Soap11Client(IHttpClientFactory clientFactory, IOptionsMonitor<ClientCertificateOptions> optionsAccessor)
            : base(clientFactory, optionsAccessor, "http://schemas.xmlsoap.org/soap/envelope/", "text/xml")
        {
        }

        public override HttpRequestMessage BuildRequest(RequestSettings requestSettings)
        {
            var httpRequestMessage = base.BuildRequest(requestSettings);
            requestSettings.SoapAction = "\"" + (string.IsNullOrEmpty(requestSettings.SoapAction) ? null : requestSettings.SoapAction) + "\"";
            httpRequestMessage.Headers.Add("SOAPAction", requestSettings.SoapAction);
            return httpRequestMessage;
        }
    }
}
