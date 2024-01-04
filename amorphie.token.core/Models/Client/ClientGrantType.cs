
using System.Text.Json.Serialization;


namespace amorphie.token.core.Models.Client;

public class ClientGrantType
{
    [JsonPropertyName("grantType")]
    public string GrantType { get; set; }
}
