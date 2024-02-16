using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using amorphie.token.core.Enums;

namespace amorphie.token.core.Constants
{
    public static class TokenConstants
    {
        public static Dictionary<LogonChannel,string> LogonChannelMap = new Dictionary<LogonChannel, string>()
        {
            {LogonChannel.OnMobil,"On Mobil"}
        };
    }
}