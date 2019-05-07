using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;

namespace Client.Settings
{
    public class ClientCertificateOptionsPostConfigure : IPostConfigureOptions<ClientCertificateOptions>
    {
        public void PostConfigure(string name, ClientCertificateOptions options)
        {
            if (options.ClientCertificate == null)
            {
                options.ClientCertificate = new X509Certificate2(options.ClientCertificatePath, options.ClientCertificatePassword, X509KeyStorageFlags.MachineKeySet);
            }
        }
    }
}
