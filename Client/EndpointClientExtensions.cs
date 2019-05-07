using Client.Clients;
using Client.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

namespace Client
{
    public static class EndpointClientExtensions
    {
        public static IServiceCollection AddEndpointClients(this IServiceCollection services)
        {
            services.AddOptions();
            services.AddSingleton<IPostConfigureOptions<ClientCertificateOptions>, ClientCertificateOptionsPostConfigure>();

            services.AddHttpClient();

            services.AddSingleton<Soap11Client>();
            services.AddSingleton<Soap12Client>();
            services.AddSingleton<RestClient>();

            services.AddSingleton<Func<string, IEndpointClient>>(sp => endpointType =>
            {
                switch (endpointType)
                {
                    case "SOAP11":
                        return sp.GetRequiredService<Soap11Client>();
                    case "SOAP12":
                        return sp.GetRequiredService<Soap12Client>();
                    case "REST":
                        return sp.GetRequiredService<RestClient>();
                    default:
                        throw new NotImplementedException($"No implementation for endpoint type: {endpointType}");
                }
            });

            return services;
        }

        public static IServiceCollection AddEndpointClients(this IServiceCollection services, Action<ClientCertificateOptions> configureOptions)
        {
            services.AddEndpointClients();
            if (configureOptions != null) services.Configure(configureOptions);
            return services;
        }
    }
}
