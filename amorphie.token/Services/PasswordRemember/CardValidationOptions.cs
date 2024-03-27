using System.Text.Json.Serialization;

namespace amorphie.token;

public class CardValidationOptions
{
    [JsonPropertyName("client_id")]
    public string? ClientId { get; set; }
    [JsonPropertyName("client_secret")]
    public string? ClientSecret { get; set; }
    [JsonPropertyName("grant_type")]
    public string? GrantType { get; set; }
    [JsonPropertyName("scopes")]
    public IEnumerable<string>? Scopes { get; set; }
}
