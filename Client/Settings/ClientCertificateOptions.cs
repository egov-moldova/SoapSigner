using System.Security.Cryptography.X509Certificates;

namespace Client.Settings
{
    public class ClientCertificateOptions
    {
        public string ClientCertificatePath { get; set; }
        public string ClientCertificatePassword { get; set; }
        public X509Certificate2 ClientCertificate { get; set; }
    }
}
