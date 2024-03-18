using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using amorphie.token.core.Enums;

namespace amorphie.token.core.Constants
{
    public class ErrorMessages
    {
        public static readonly Dictionary<string, string> OtpNotValid = new Dictionary<string, string>()
        {
            {"tr-TR","Doğrulama Kodu Hatalı"},
            {"en-EN","Verification Code is Not Valid"},
            {"en-US","Verification Code is Not Valid"},
        };

        public static readonly Dictionary<string, string> General = new Dictionary<string, string>()
        {
            {"tr-TR","Teknik Bir Sorun Oluştu. Daha Sonra Tekrar Deneyin"},
            {"en-EN","A technical problem occurred, Please Try Again Later"},
            {"en-US","A technical problem occurred, Please Try Again Later"},
        };

        public static readonly Dictionary<string, string> LoginUserNotFound = new Dictionary<string, string>()
        {
            {"tr-TR","Kullanıcı Bulunamadı"},
            {"en-EN","User Not Found"},
            {"en-US","User Not Found"},
        };

        public static readonly Dictionary<string, string> LoginWrongPassword = new Dictionary<string, string>()
        {
            {"tr-TR","Kullanıcı Adı veya Şifre Hatalı"},
            {"en-EN","Wrong Username or Password"},
            {"en-US","Wrong Username or Password"},
        };

        public static readonly Dictionary<string, string> LoginBlockedUser = new Dictionary<string, string>()
        {
            {"tr-TR","Kullanıcı Blokeli"},
            {"en-EN","User is Blocked"},
            {"en-US","User is Blocked"},
        };

        public static readonly Dictionary<string, string> LoginBlockUser = new Dictionary<string, string>()
        {
            {"tr-TR","Çok Fazla Hatalı Giriş Sonrası Kullanıcı Blokelendi"},
            {"en-EN","User is Blocked Now After Several Login Attemps"},
            {"en-US","User is Blocked Now After Several Login Attemps"},
        };

        public static readonly Dictionary<string, string> SamePassword = new Dictionary<string, string>()
        {
            {"tr-TR","Yeni şifreniz son 5 şifreniz ile aynı olamaz"},
            {"en-EN","New password couldn't be same as your last 5 passwords"},
            {"en-US","New password couldn't be same as your last 5 passwords"},
        };

        public static readonly Dictionary<LoginErrors,Dictionary<string,string>> LoginErrorMap = new()
        {
            {LoginErrors.UserNotFound,LoginUserNotFound},
            {LoginErrors.WrongPassword,LoginWrongPassword},
            {LoginErrors.WrongOtp,OtpNotValid},
            {LoginErrors.BlockedUser,LoginBlockedUser},
            {LoginErrors.BlockUser,LoginBlockUser},
            {LoginErrors.SamePassword,SamePassword},
            {LoginErrors.General,General}
        };
    }
}