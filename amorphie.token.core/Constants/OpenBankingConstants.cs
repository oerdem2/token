using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace amorphie.token.core.Constants
{
    public static class OpenBankingConstants
    {
        public static Dictionary<string, string> ConsentTypeMap = new Dictionary<string, string>()
        {
            {"OB_Account","H"},
            {"OB_Payment","O"},
        };
    }
}