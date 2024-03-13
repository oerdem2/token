
using System.Text.Json.Serialization;


namespace amorphie.token.core.Models.Token;

public class TokenRequest
{
    [JsonPropertyName("username")]
    public string? Username { get; set; }
    [JsonPropertyName("password")]
    public string? Password { get; set; }
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }
    [JsonPropertyName("client_id")]
    public string? ClientId { get; set; }
    [JsonPropertyName("client_secret")]
    public string? ClientSecret { get; set; }
    [JsonPropertyName("code")]
    public string? Code { get; set; }
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }
    [JsonPropertyName("grant_type")]
    public string? GrantType { get; set; }
    [JsonPropertyName("redirect_uri")]
    public string? RedirectUri { get; set; }
    [JsonPropertyName("code_verifier")]
    public string? CodeVerifier { get; set; }
    [JsonPropertyName("record_id")]
    public string? RecordId { get; set; }
    [JsonPropertyName("scopes")]
    public IEnumerable<string>? Scopes { get; set; }
    [JsonPropertyName("device_id")]
    public string? DeviceId { get; set; }
    [JsonPropertyName("installation_id")]
    public string? InstallationId { get; set; }


}
