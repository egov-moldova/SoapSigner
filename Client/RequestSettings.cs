using System;
using System.Net.Http;

namespace Client
{
    public class RequestSettings
    {
        public HttpMethod Method { get; set; }
        public Uri Uri { get; set; }
        public string RequestHeaders { get; set; }
        public string SoapAction { get; set; }
        public string ContentHeaders { get; set; }
        public string Content { get; set; }
        public bool SignMessage { get; set; }
    }
}
