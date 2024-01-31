
using System.Text.Json.Serialization;

namespace amorphie.token.core.Models.User
{
    public class RememberPassword
    {
        [JsonPropertyName("username")]
        public string? Username{get;set;}
        [JsonPropertyName("phone")]
        public string? Phone{get;set;}
        [JsonPropertyName("client_id")]
        public string? ClientId{get;set;}
        [JsonPropertyName("client_secret")]
        public string? ClientSecret{get;set;}
    }
}