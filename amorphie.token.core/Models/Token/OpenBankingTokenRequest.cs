using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace amorphie.token.core.Models.Token;

public class OpenBankingTokenRequest
{
    [JsonPropertyName("rizaNo")]
    public string? ConsentNo { get; set; }
    [JsonPropertyName("rizaTip")]
    public string? ConsentType { get; set; }
    [JsonPropertyName("yetTip")]
    public string? AuthType { get; set; }
    [JsonPropertyName("yetKod")]
    public string? AuthCode { get; set; }
    [JsonPropertyName("yenilemeBelirteci")]
    public string? RefreshToken { get; set; }

}
