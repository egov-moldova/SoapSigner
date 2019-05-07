using Client.Settings;
using Microsoft.Extensions.Options;
using System.Net.Http;

namespace Client.Clients
{
    public sealed class Soap12Client : SoapClient
    {
        public Soap12Client(IHttpClientFactory clientFactory, IOptionsMonitor<ClientCertificateOptions> optionsAccessor)
            : base(clientFactory, optionsAccessor, "http://www.w3.org/2003/05/soap-envelope", "application/soap+xml")
        {
        }
    }
}
