using System.Dynamic;
using System.Text;
using System.Text.Json;
using amorphie.token.core.Models.InternetBanking;
using amorphie.token.data;
using Microsoft.AspNetCore.Mvc;

namespace amorphie.token;

public static class RememberPasswordSms
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public static async Task<IResult> SendTempPassworSms(
        [FromBody] dynamic body,
        [FromServices] IbDatabaseContext ibContext,
        IConfiguration configuration,
         [FromServices] IEkycService ekycService
    )
    {

        var transitionName = body.GetProperty("LastTransition").ToString();
        var dataBody = body.GetProperty($"TRX-{transitionName}").GetProperty("Data");
        dynamic dataChanged = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(dataBody.ToString());

        var ibUserSerialized = body.GetProperty("ibUserSerialized").ToString();
        IBUser ibUser = JsonSerializer.Deserialize<IBUser>(ibUserSerialized);

        var userInfoSerialized = body.GetProperty("userSerialized").ToString();
        LoginResponse userInfo = JsonSerializer.Deserialize<LoginResponse>(userInfoSerialized, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });


        dynamic variables = new Dictionary<string, dynamic>();
        dataChanged.additionalData = new ExpandoObject();

        // Generate a temp password and send it the user via sms
        // Generate a temp password
        var tempPassword = GenerateTempPassword(6);



        PasswordHasher passwordHasher = new();
        IBPassword password = new IBPassword
        {

            AccessFailedCount = 0,
            CreatedByUserName = "Amorphie",
            UserId = ibUser.Id,
            MustResetPassword = true
        };

        password.HashedPassword = passwordHasher.HashPassword(tempPassword, password.Id.ToString());
        await ibContext.Password.AddAsync(password);

        // Now send the temp password via sms

        var otpRequest = new
        {
            Sender = "AutoDetect",
            SmsType = "Otp",
            Phone = new
            {
                CountryCode = userInfo.MobilePhone!.CountryCode,
                Prefix = userInfo.MobilePhone.Prefix,
                Number = userInfo.MobilePhone.Number
            },
            Content = $"{tempPassword} şifresi ile giriş yapabilirsiniz",
            Process = new
            {
                Name = "Remember Password Temp Password",
                Identity = "Remember Password"
            }
        };

        StringContent request = new(JsonSerializer.Serialize(otpRequest), Encoding.UTF8, "application/json");

        using var httpClient = new HttpClient();
        var httpResponse = await httpClient.PostAsync(configuration["MessagingGatewayUri"], request);

            if (httpResponse.IsSuccessStatusCode)
            {

                variables.SmsStatus = true;
                
            }
            else
            {
                variables.SmsStatus = false;
               
            }

        return Results.Ok(variables);
    }


    private static string GenerateTempPassword(int length)
    {
        const string upperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lowerChars = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string allChars = upperChars + lowerChars + digits;

        Random random = new Random();
        StringBuilder password = new StringBuilder();

        // Ensure at least one character from each set
        password.Append(upperChars[random.Next(upperChars.Length)]);
        password.Append(lowerChars[random.Next(lowerChars.Length)]);
        password.Append(digits[random.Next(digits.Length)]);

        // Fill the rest of the password length with random characters from allChars
        for (int i = 3; i < length; i++)
        {
            password.Append(allChars[random.Next(allChars.Length)]);
        }

        // Shuffle the password to ensure the first 3 characters are not predictable
        return ShuffleString(password.ToString(), random);
    }

    static string ShuffleString(string input, Random random)
    {
        char[] array = input.ToCharArray();
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            char temp = array[i];
            array[i] = array[j];
            array[j] = temp;
        }
        return new string(array);
    }
}
