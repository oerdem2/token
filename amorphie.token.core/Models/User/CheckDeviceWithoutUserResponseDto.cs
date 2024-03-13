using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace amorphie.token.core.Models.User
{
    public class CheckDeviceWithoutUserResponseDto
    {
        [JsonPropertyName("reference")]
        public string? Reference { get; set; }
    }
}