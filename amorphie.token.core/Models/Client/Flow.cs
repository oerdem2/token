
using System.Text.Json.Serialization;

namespace amorphie.token.core.Models.Client;

public class Flow
{
    public string type { get; set; }
    public string workflow { get; set; }

    [JsonPropertyName("duration")]
    public string tokenduration { get; set; }
    public string token { get; set; }
}
