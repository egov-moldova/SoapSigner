using Client.Settings;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;

namespace Client.Clients
{
    public abstract class SoapClient : BaseClient
    {
        private readonly string soapNamespace;
        private readonly string contentType;

        protected SoapClient(IHttpClientFactory clientFactory, IOptionsMonitor<ClientCertificateOptions> optionsAccessor, string soapNamespace, string contentType)
            : base(clientFactory, optionsAccessor)
        {
            this.soapNamespace = soapNamespace;
            this.contentType = contentType;
        }

        public override HttpRequestMessage BuildRequest(RequestSettings requestSettings)
        {
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(requestSettings.Uri.ToString()));
            FillHeaders(requestSettings.RequestHeaders, httpRequestMessage.Headers);
            httpRequestMessage.Content = new StringContent(CreateXmlMessage(requestSettings.Content, requestSettings.SignMessage), Encoding.UTF8, contentType);
            FillHeaders(requestSettings.ContentHeaders, httpRequestMessage.Content.Headers);
            return httpRequestMessage;
        }

        private string CreateXmlMessage(string requestBody, bool signMessage)
        {
            if (!signMessage)
            {
                return $@"<soap:Envelope xmlns:soap=""{soapNamespace}"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""><soap:Body>{requestBody}</soap:Body></soap:Envelope>";
            }

            var doc = new XmlDocument();
            var envelope = doc.CreateElement("soap", "Envelope", soapNamespace);
            envelope.SetAttribute("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
            doc.AppendChild(envelope);

            // Create Security Header
            var id = Guid.NewGuid().ToString("N");
            var TimestampNs = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";

            var security = doc.CreateElement("Security", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd");
            var msAtt = doc.CreateAttribute("soap", "mustUnderstand", soapNamespace);
            msAtt.InnerText = "1";
            security.Attributes.Append(msAtt);

            var timestamp = doc.CreateElement("Timestamp", TimestampNs);
            timestamp.SetAttribute("Id", "TS-" + id);
            security.AppendChild(timestamp);

            var created = doc.CreateElement("Created", TimestampNs);
            created.InnerText = XmlConvert.ToString(DateTimeOffset.UtcNow);
            timestamp.AppendChild(created);

            var expires = doc.CreateElement("Expires", TimestampNs);
            expires.InnerText = XmlConvert.ToString(DateTimeOffset.UtcNow.AddMinutes(15));
            timestamp.AppendChild(expires);

            var header = doc.CreateElement("soap", "Header", soapNamespace);
            header.AppendChild(security);
            envelope.AppendChild(header);

            // Create Body
            var body = doc.CreateElement("soap", "Body", soapNamespace);
            envelope.AppendChild(body);
            var bodyDocument = new XmlDocument();
            try
            {
                bodyDocument.LoadXml(requestBody);
            }
            catch
            {
                throw new ApplicationException("Invalid XML for SOAP Body");
            }
            body.AppendChild(body.OwnerDocument.ImportNode(bodyDocument.DocumentElement, true));
            body.SetAttribute("Id", "MS-" + id);

            return ApplySignature(doc, header, body, id);
        }

        public override JToken ProcessResponseBody(ResponseSettings responseSettings, string responseBody)
        {
            try
            {
                var xmlDocument = new XmlDocument
                {
                    PreserveWhitespace = true
                };
                xmlDocument.LoadXml(responseBody);
                var bodyNodes = xmlDocument.GetElementsByTagName("Body", soapNamespace);
                if (bodyNodes.Count != 1) throw new ApplicationException("No or more than one SOAP Body in response");
                var body = bodyNodes[0];
                if (body.FirstChild == null) throw new ApplicationException("No child in SOAP Body");
                if (responseSettings.MessageSigned)
                {
                    ValidateSignature(xmlDocument, responseSettings.ServiceCertificate);
                }
                return JObject.Parse(JsonConvert.SerializeXmlNode(body.FirstChild));
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Invalid SOAP Message in response", ex);
            }
        }

        public override XmlNode GetResponseBody(ResponseSettings responseSettings, string responseBody)
        {
            try
            {
                var xmlDocument = new XmlDocument
                {
                    PreserveWhitespace = true
                };
                xmlDocument.LoadXml(responseBody);
                var bodyNodes = xmlDocument.GetElementsByTagName("Body", soapNamespace);
                if (bodyNodes.Count != 1) throw new ApplicationException("No or more than one SOAP Body in response");
                var body = bodyNodes[0];
                if (body.FirstChild == null) throw new ApplicationException("No child in SOAP Body");
                if (responseSettings.MessageSigned)
                {
                    ValidateSignature(xmlDocument, responseSettings.ServiceCertificate);
                }
                return body.FirstChild;
                //return JObject.Parse(JsonConvert.SerializeXmlNode(body.FirstChild));
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Invalid SOAP Message in response", ex);
            }
        }

        private string ApplySignature(XmlDocument doc, XmlElement header, XmlElement body, string id)
        {
            body.SetAttribute("Id", "MS-" + id);

            var keyInfo = new KeyInfo();
            keyInfo.AddClause(new KeyInfoX509Data(Options.ClientCertificate));
            var signedXml = new SignedXml(doc)
            {
                KeyInfo = keyInfo,
                SigningKey = Options.ClientCertificate.PrivateKey
            };

            signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;
            var bodyReference = new Reference
            {
                Uri = "#MS-" + id
            };
            bodyReference.AddTransform(new XmlDsigExcC14NTransform());  // required to match doc
            signedXml.AddReference(bodyReference);

            Reference tsReference = new Reference
            {
                Uri = "#TS-" + id
            };
            tsReference.AddTransform(new XmlDsigExcC14NTransform());  // required to match doc
            signedXml.AddReference(tsReference);

            signedXml.ComputeSignature();
            var signedElement = signedXml.GetXml();

            header.FirstChild.AppendChild(signedElement);
            return doc.InnerXml;
        }

        private void ValidateSignature(XmlDocument doc, X509Certificate2 serviceCertificate)
        {
            var signatureNodes = doc.GetElementsByTagName("Signature", "http://www.w3.org/2000/09/xmldsig#");
            if (signatureNodes.Count != 1) throw new ApplicationException("No or more than one SOAP Signature in response");
            var sdoc = new SignedSoapXml(doc.DocumentElement);
            sdoc.LoadXml((XmlElement)signatureNodes[0]);
            if (!sdoc.CheckSignature(serviceCertificate, true)) throw new ApplicationException("Invalid SOAP Signature");
        }

        private class SignedSoapXml : SignedXml
        {
            public SignedSoapXml(XmlElement elem) : base(elem)
            {
            }

            public override XmlElement GetIdElement(XmlDocument document, string idValue)
            {
                var nodes = document.SelectNodes("//*[@*[local-name()='Id' and .='" + idValue + "']]");
                if ((nodes == null) || (nodes.Count != 1)) return null;
                return nodes[0] as XmlElement;
            }
        }
    }
}
