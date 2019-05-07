using System.Security.Cryptography.X509Certificates;

namespace Client
{
    public class ResponseSettings
    {
        public bool MessageSigned { get; set; }
        public X509Certificate2 ServiceCertificate { get; set; }
    }
}
