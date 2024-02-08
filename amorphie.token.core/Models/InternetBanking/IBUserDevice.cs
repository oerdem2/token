using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace amorphie.token.core.Models.InternetBanking
{
    public class IBUserDevice
    {
        public Guid UserId { get; set; }
        public string DeviceId { get; set; }
        public string InstallationId { get; set; }
        public string DeviceToken { get; set; }
        public string Model { get; set; }
        public string Platform { get; set; }
        public string Version { get; set; }
        public string Manufacturer { get; set; }
        public string Description { get; set; }
        public string RemovalReason { get; set; }
        public int Status { get; set; }
        public DateTime? RegistrationDate { get; set; }
        public DateTime? ActivationDate { get; set; }
        public DateTime? ActivationRemovalDate { get; set; }
        public DateTime? LastLogonDate { get; set; }
        public bool? IsGoogleServiceAvailable { get; set; }
        public bool? IsOnApp { get; set; }
    }
}