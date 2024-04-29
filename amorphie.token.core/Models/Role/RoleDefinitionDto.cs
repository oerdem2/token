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
        public IEnumerable<string> Tags{get;set;}
    }
}