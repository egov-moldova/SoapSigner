using Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SaopSigner.Models;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace SaopSigner.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration _configuration;

        public HomeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SignMessage(MessageModel model, [FromServices] Func<string, IEndpointClient> clientFactory)
        {
            //Url to calling service
            var endpointUrl = model.Endpoint;
            //Message type (SOAP11 or SOAP12) 
            var endpointClient = clientFactory(model.Type);
            //Message  building and signing for sending to service
            var request = endpointClient.BuildRequest(new RequestSettings
            {
                Uri = new Uri(endpointUrl),
                RequestHeaders = model.RequestHeaders,
                SoapAction = model.SoapAction,
                ContentHeaders = "",
                Content = model.RequestBody,
                SignMessage = true
            });
            model.Request = await request.Content.ReadAsStringAsync();
            try
            {
                //Send message to service 
                var httpResponse = await endpointClient.SendRequest(request, 45000);
                model.Response = await httpResponse.Content.ReadAsStringAsync();
                //Verifying received message with service provider certificate and getting the message body in xml format
                //For getting message body in json format use method ProcessResponseBody(responseSettingsm, responseBody)
                model.ResponseBody = endpointClient.GetResponseBody(new ResponseSettings()
                {
                    MessageSigned = true,
                    ServiceCertificate =  new X509Certificate2(_configuration.GetSection("Settings:ServiceCertificate:Path").Value) 
                },
                   model.Response).InnerXml;
            }
            catch(Exception e)
            {
                model.Response = e.Message.ToString();
            }
            return View("Index", model);
        }
    }
}
