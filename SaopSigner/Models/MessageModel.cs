namespace SaopSigner.Models
{
    public class MessageModel
    {
        public string Endpoint { get; set; }
        public string SoapAction { get; set; }
        public string RequestHeaders { get; set; }
        public string RequestBody { get; set; }
        public string Request { get; set; }
        public string Response { get; set; }
        public string ResponseBody { get; set; }
        public string Type { get; set; }
    }
}
