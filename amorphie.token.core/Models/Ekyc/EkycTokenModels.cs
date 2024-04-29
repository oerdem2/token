namespace amorphie.token.core;

public class EkycTokenModels
{

    public class Request
    {
        public string UserName { get; set; }
        public string Password { get; set; }
    }

    public class Response : EkycResponseBase
    {
        public string Token { get; set; }
        public string Expires { get; set; }
        public string Validity { get; set; }
        public Result Result { get; set; }
    }
}
