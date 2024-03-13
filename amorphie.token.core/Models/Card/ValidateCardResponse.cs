using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace amorphie.token.core.Models.Card
{
    public class ValidateCardResponse
    {
        [JsonPropertyName("textDescription")]
        public string? TextDescription { get; set; }
        [JsonPropertyName("isSuccess")]
        public bool IsSuccess { get; set; }
    }
}