namespace amorphie.token.core;

public class GetIntegrationInfoModels
{
    public class Request
    {
        public List<string> Types { get; set; }
        public string Reference { get; set; }
        public string IdentityType { get; set; }
        public string IdentityNo { get; set; }
        public string SessionUId { get; set; }
        public List<string> Statuses { get; set; }
    }

    public class Response : EkycResponseBase
    {
        public List<Data> Data { get; set; }
    }

    public class Data
    {
        public Guid UId { get; set; }
        public string Type { get; set; }
        public string Reference { get; set; }
        public string CallType { get; set; }
        public Guid? SessionUId { get; set; }
        public string Status { get; set; }
        public string IdentityType { get; set; }
        public string IdentityNo { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
    }
}
