namespace amorphie.token.core;

public class EkycCreateSessionResultModel
{
    public string Name { get; set; }
    public string Surname { get; set; }
    public string CallType { get; set; }

    public string ReferenceId { get; set; }
    public bool IsSuccessful { get; set; }
}
