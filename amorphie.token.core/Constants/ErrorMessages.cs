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
            {"tr-TR","Şifre doğrulama kodunu hatalı girdiniz, yeniden deneyin."},
            {"en-EN","You entered the password verification code incorrectly, try again."},
            {"en-US","You entered the password verification code incorrectly, try again."},
        };

        public static readonly Dictionary<string, string> OtpTimeout = new Dictionary<string, string>()
        {
            {"tr-TR","Şifre doğrulama kodunun süresi dolmuştur. Yeniden gönderiniz."},
            {"en-EN","Password verification code has been expired, send again."},
            {"en-US","Password verification code has been expired, send again."},
        };

        public static readonly Dictionary<string, string> General = new Dictionary<string, string>()
        {
            {"tr-TR","Teknik Bir Sorun Oluştu. Daha Sonra Tekrar Deneyin"},
            {"en-EN","A technical problem occurred, Please Try Again Later"},
            {"en-US","A technical problem occurred, Please Try Again Later"},
        };

        public static readonly Dictionary<string, string> LoginUserNotFound = new Dictionary<string, string>()
        {
            {"tr-TR","Yanlış şifre girdiniz, lütfen tekrar deneyin."},
            {"en-EN","You entered the wrong password, please try again."},
            {"en-US","You entered the wrong password, please try again."},
        };

        public static readonly Dictionary<string, string> LoginWrongPassword = new Dictionary<string, string>()
        {
            {"tr-TR","Yanlış şifre girdiniz, lütfen tekrar deneyin."},
            {"en-EN","You entered the wrong password, please try again."},
            {"en-US","You entered the wrong password, please try again."},
        };

        public static readonly Dictionary<string, string> LoginBlockedUser = new Dictionary<string, string>()
        {
            {"tr-TR","Şifreyi 5 kere yanlış girdiğiniz için giriş yapılamıyor. Şifremi Unuttum butonuna basarak yeni şifre alabilir, giriş yapabilirsiniz."},
            {"en-EN","You cannot log in because you entered the wrong password 5 times. You can get a new password and log in by pressing the Forgot My Password button."},
            {"en-US","You cannot log in because you entered the wrong password 5 times. You can get a new password and log in by pressing the Forgot My Password button."},
        };

        public static readonly Dictionary<string, string> LoginBlockUser = new Dictionary<string, string>()
        {
            {"tr-TR","Çok Fazla Hatalı Giriş Sonrası Kullanıcı Blokelendi"},
            {"en-EN","User is Blocked Now After Several Login Attemps"},
            {"en-US","User is Blocked Now After Several Login Attemps"},
        };

        public static readonly Dictionary<string, string> SamePassword = new Dictionary<string, string>()
        {
            {"tr-TR","Önceki 5 şifrenizi tekrar kullanamazsınız, lütfen başka bir şifre belirleyin."},
            {"en-EN","You cannot reuse your previous 5 passwords, please set another password."},
            {"en-US","You cannot reuse your previous 5 passwords, please set another password."},
        };

        public static readonly Dictionary<LoginErrors, Dictionary<string, string>> LoginErrorMap = new()
        {
            { LoginErrors.UserNotFound, LoginUserNotFound },
            { LoginErrors.WrongPassword, LoginWrongPassword },
            { LoginErrors.WrongOtp, OtpNotValid },
            { LoginErrors.OtpTimeout, OtpTimeout },
            { LoginErrors.BlockedUser, LoginBlockedUser },
            { LoginErrors.BlockUser, LoginBlockUser },
            { LoginErrors.SamePassword, SamePassword },
            { LoginErrors.General, General }
        };
    }
}