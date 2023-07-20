using System.Text.Json.Serialization;
using token.Models.Client;

namespace AuthServer.Models.Client;

public class ClientResponse
{
    public string id { get; set; }
    public string name { get; set; }
    public List<Flow> flows { get; set; }
    [JsonPropertyName("allowedGrantTypes")]
    public ICollection<ClientGrantType> allowedgranttypes{get;set;}
    [JsonPropertyName("allowedScopeTags")]
    public List<string> allowedscopetags { get; set; }

    [JsonPropertyName("loginUrl")]
    public string loginurl { get; set; }

    [JsonPropertyName("returnUrl")]
    public string returnuri { get; set; }

    [JsonPropertyName("logoutUrl")]
    public string logouturi { get; set; }

    [JsonPropertyName("clientSecret")]
    public string clientsecret { get; set; }
    public string pkce { get; set; }
    public Jws jws { get; set; }
    public Idempotency idempotency { get; set; }
    public Variant variant { get; set; }
    public Session session { get; set; }
    public Location location { get; set; }
    public List<Token> tokens { get; set; }
}
