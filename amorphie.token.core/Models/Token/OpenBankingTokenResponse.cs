using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace amorphie.token.core.Models.Token;

public class OpenBankingTokenResponse
{
    [JsonPropertyName("erisimBelirteci")]
    public string? AccessToken { get; set; }
    [JsonPropertyName("gecerlilikSuresi")]
    public int ExpiresIn { get; set; }
    [JsonPropertyName("yenilemeBelirteci")]
    public string? RefreshToken { get; set; }
    [JsonPropertyName("yenilemeBelirteciGecerlilikSuresi")]
    public int RefreshTokenExpiresIn { get; set; }

}
