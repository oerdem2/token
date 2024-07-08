using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace amorphie.token.core.Models.RememberPassword
{
    public class VideoCallAvailableResponse
    {
        [JsonPropertyName("IsActive")]
        public bool IsActive { get; set; }
         [JsonPropertyName("IsActiveInSpecificHours")]
        public bool IsActiveInSpecificHours { get; set; }
        [JsonPropertyName("ActiveStartHour")]
        public string ActiveStartHour { get; set; }

        [JsonPropertyName("ActiveDueHour")]
        public string ActiveDueHour { get; set; }
    }
}