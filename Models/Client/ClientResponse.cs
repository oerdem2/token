using System.Text.Json.Serialization;

namespace AuthServer.Models.Client;

public class ClientResponse
{
    public string id { get; set; }
    public string name { get; set; }
    public List<Flow> flows { get; set; }

    [JsonPropertyName("allowed-scope-tags")]
    public List<string> allowedscopetags { get; set; }

    [JsonPropertyName("login-url")]
    public string loginurl { get; set; }

    [JsonPropertyName("return-url")]
    public string returnuri { get; set; }

    [JsonPropertyName("logout-url")]
    public string logouturi { get; set; }

    [JsonPropertyName("client-secret")]
    public string clientsecret { get; set; }
    public string pkce { get; set; }
    public Jws jws { get; set; }
    public Idempotency idempotency { get; set; }
    public Variant variant { get; set; }
    public Session session { get; set; }
    public Location location { get; set; }
    public List<Token> tokens { get; set; }
}
