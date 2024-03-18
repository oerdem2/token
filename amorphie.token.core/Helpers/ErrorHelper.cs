using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using amorphie.token.core.Constants;
using amorphie.token.core.Enums;
using Elastic.CommonSchema;

namespace amorphie.token.core.Helpers
{
    public class ErrorHelper
    {
        public static string GetErrorMessage(LoginErrors loginError,string lang)
        {
            if(ErrorMessages.LoginErrorMap.ContainsKey(loginError))
            {
                return ErrorMessages.LoginErrorMap[loginError].ContainsKey(lang) ?
                ErrorMessages.LoginErrorMap[loginError][lang] :
                ErrorMessages.LoginErrorMap[loginError]["tr-TR"];
            }
            else
            {
                return ErrorMessages.LoginErrorMap[LoginErrors.General].ContainsKey(lang) ?
                ErrorMessages.LoginErrorMap[LoginErrors.General][lang] :
                ErrorMessages.LoginErrorMap[LoginErrors.General]["tr-TR"];
            }
        }

        public static string GetLangCode(dynamic body)
        {
            try
            {
                var headers = body.GetProperty("Headers");
                var lang = headers.GetProperty("acceptlanguage");
                if(string.IsNullOrWhiteSpace(lang))
                    return "tr-TR";
                return lang;
            }
            catch (Exception ex)
            {
                return "tr-TR";
            }
        }
    }

    
}