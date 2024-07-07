using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace amorphie.token.core.Models.Role
{
    public class RoleDefinitionDto
    {
        [JsonPropertyName("id")]
        public Guid Id{get;set;}
        [JsonPropertyName("tags")]
        public string[]? Tags { get; set; }
        public string? Status { get; set; }
        public int Key { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}