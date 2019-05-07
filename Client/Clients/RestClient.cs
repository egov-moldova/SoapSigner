using Jose;
using Client.Settings;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;

namespace Client.Clients
{
    public sealed class RestClient : BaseClient
    {
        public RestClient(IHttpClientFactory clientFactory, IOptionsMonitor<ClientCertificateOptions> optionsAccessor)
            : base(clientFactory, optionsAccessor)
        {
        }

        public override HttpRequestMessage BuildRequest(RequestSettings requestSettings)
        {                                                      
            var httpRequestMessage = new HttpRequestMessage
            {
                RequestUri = requestSettings.Uri,
                Method = requestSettings.Method
            };                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          
            FillHeaders(requestSettings.RequestHeaders, httpRequestMessage.Headers);
            var content = requestSettings.Content;
            if (requestSettings.SignMessage)
            {
                try
                {
                    JToken.Parse(content);
                }
                catch
                {
                    throw new ApplicationException("Invalid JSON message");
                }

                content = ApplySignature(requestSettings.Content);
            }
            httpRequestMessage.Content = new StringContent(content, Encoding.UTF8, "application/json");
            FillHeaders(requestSettings.ContentHeaders, httpRequestMessage.Content.Headers);
            return httpRequestMessage;
        }

        private string ApplySignature(string content)
        {
            var privateKey = Options.ClientCertificate.GetRSAPrivateKey();
            return JWT.Encode(content, privateKey, JwsAlgorithm.RS256);
        }

        public override JToken ProcessResponseBody(ResponseSettings responseSettings, string responseBody)
        {
            
            try
            {
                if (responseSettings.MessageSigned)
                {
                    return JToken.Parse(ValidateSignature(responseBody, responseSettings.ServiceCertificate));
                }
                return JToken.Parse(responseBody);
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Invalid JSON in response", ex);
            }
        }

        private string ValidateSignature(string responseBody, X509Certificate2 serviceCertificate)
        {
            var publicKey = serviceCertificate.GetRSAPublicKey();
            try
            {
                return JWT.Decode(responseBody, publicKey);
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message);
            }
        }

        public override XmlNode GetResponseBody(ResponseSettings responseSettings, string responseBody)
        {
            throw new NotImplementedException();
        }
    }
}