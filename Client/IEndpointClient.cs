using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;

namespace Client
{
    public interface IEndpointClient
    {
        HttpRequestMessage BuildRequest(RequestSettings requestSettings);
        Task<HttpResponseMessage> SendRequest(HttpRequestMessage request, long timeout = 0);
        Task<JToken> ProcessResponse(ResponseSettings responseSettings, HttpResponseMessage response);
        JToken ProcessResponseBody(ResponseSettings responseSettings, string responseBody);
        XmlNode GetResponseBody(ResponseSettings responseSettings, string responseBody);
    }
}
