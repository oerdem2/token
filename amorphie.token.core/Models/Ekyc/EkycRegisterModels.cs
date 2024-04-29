namespace amorphie.token.core;

public class EkycRegisterModels
{
    public class Request
    {
        public string Type { get; set; }
        public string Reference { get; set; }
        public string CallType { get; set; }
        public string Data { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string FormUrl { get; set; }
        public IdRegistration IDRegistration { get; set; }
    }

    public class Response : EkycResponseBase
    {
        public Result Result { get; set; }
        public Result Warning { get; set; }
    }
}
