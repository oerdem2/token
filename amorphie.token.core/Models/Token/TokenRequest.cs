
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;


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


public class TokenRequestForm
{
    [FromForm(Name = "username")]
    public string? Username { get; set; }
    [FromForm(Name = "password")]
    public string? Password { get; set; }
    [FromForm(Name = "phone")]
    public string? Phone { get; set; }
    [FromForm(Name = "client_id")]
    public string? ClientId { get; set; }
    [FromForm(Name = "client_secret")]
    public string? ClientSecret { get; set; }
    [FromForm(Name = "code")]
    public string? Code { get; set; }
    [FromForm(Name = "refresh_token")]
    public string? RefreshToken { get; set; }
    [FromForm(Name = "grant_type")]
    public string? GrantType { get; set; }
    [FromForm(Name = "redirect_uri")]
    public string? RedirectUri { get; set; }
    [FromForm(Name = "code_verifier")]
    public string? CodeVerifier { get; set; }
    [FromForm(Name = "scopes")]
    public IEnumerable<string>? Scopes { get; set; }
    [FromForm(Name = "device_id")]
    public string? DeviceId { get; set; }
    [FromForm(Name = "installation_id")]
    public string? InstallationId { get; set; }


}