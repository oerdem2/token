using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace amorphie.token.core.Models.Consent
{
    public class ConsentResponse
    {
        public Guid id { get; set; }
        public Guid userId { get; set; }
        public string? state { get; set; }
        public string? consentType { get; set; }
        public string? additionalData { get; set; }
        public string? userTCKN{get;set;}
        public string? scopeTCKN{get;set;}
    }
}