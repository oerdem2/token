namespace amorphie.token.core.Models.Authorization;

public class OpenBankingAuthorizationRequest
{
    public Guid riza_no { get; set; }
    public string? error_message{get;set;}
}
