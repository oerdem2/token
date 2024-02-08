using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using amorphie.token.data;
using amorphie.token.Services.InternetBanking;
using amorphie.token.Services.Profile;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace amorphie.token.Modules.Login
{
    public static class CheckUserPhone
    {
        [ApiExplorerSettings(IgnoreApi = true)]
        public static async Task<IResult> checkUserPhone(
        [FromBody] dynamic body,
        [FromServices] IInternetBankingUserService internetBankingUserService,
        [FromServices] IProfileService profileService,
        [FromServices] IUserService userService,
        [FromServices] IbDatabaseContext ibContext
        )
        {
            var transitionName = body.GetProperty("LastTransition").ToString();
            var requestBodySerialized = body.GetProperty("TRX-"+transitionName).GetProperty("Data").GetProperty("entityData").ToString();
            TokenRequest request = JsonSerializer.Deserialize<TokenRequest>(requestBodySerialized);

            dynamic variables = new ExpandoObject();
            variables.PasswordTryCount = 0;
            variables.wrongCredentials = false;
            variables.disableUser = false;

            var userResponse = await internetBankingUserService.GetUser(request.Username!);
            if (userResponse.StatusCode != 200)
            {
                variables.message = "User Not Found";
                variables.wrongCredentials = true;
                return Results.Ok(variables);
            }
            var user = userResponse.Response;
            variables.ibUserSerialized = JsonSerializer.Serialize(user);

            var userStatus = await ibContext.Status.Where(s => s.UserId == user!.Id && (!s.State.HasValue || s.State.Value == 10)).OrderByDescending(s => s.CreatedAt).FirstOrDefaultAsync();
            if (userStatus?.Type == 30 || userStatus?.Type == 40)
            {
                variables.message = "User Not Active";
                variables.wrongCredentials = true;
                variables.disableUser = true;
                return Results.Ok(variables);
            }

            var userInfoResult = await profileService.GetCustomerSimpleProfile(request.Username!);
            if (userInfoResult.StatusCode != 200)
            {
                variables.message = "UserInfo Not Found";
                variables.wrongCredentials = true;
                return Results.Ok(variables);
            }

            var userInfo = userInfoResult.Response;

            if (userInfo!.data!.profile!.Equals("customer") || !userInfo!.data!.profile!.status!.Equals("active"))
            {
                variables.message = "User is Not Customer Or Not Active";
                variables.wrongCredentials = true;
                return Results.Ok(variables);
            }

            var mobilePhoneCount = userInfo!.data!.phones!.Count(p => p.type!.Equals("mobile"));
            if (mobilePhoneCount != 1)
            {
                variables.message = "Bad Phone Data";

                return Results.Ok(variables);
            }

            var mobilePhone = userInfo!.data!.phones!.FirstOrDefault(p => p.type!.Equals("mobile"));
            if (string.IsNullOrWhiteSpace(mobilePhone!.prefix) || string.IsNullOrWhiteSpace(mobilePhone!.number))
            {
                variables.message = "Bad Phone Format";
                return Results.Ok(variables);
            }

            if(!mobilePhone.ToRememberPasswordString().Equals(request.Phone))
            {
                variables.wrongCredentials = true;
                variables.message = "Phone Number Doesnt Match";
                return Results.Ok(variables);
            }

            var userRequest = new UserInfo
            {
                firstName = userInfo!.data.profile!.name!,
                lastName = userInfo!.data.profile!.surname!,
                phone = new core.Models.User.UserPhone()
                {
                    countryCode = mobilePhone!.countryCode!,
                    prefix = mobilePhone!.prefix,
                    number = mobilePhone!.number
                },
                state = "Active",
                explanation = "Migrated From IB",
                reason = "Amorphie Login",
                isArgonHash = true
            };

            var verifiedMailAddress = userInfo.data.emails!.FirstOrDefault(m => m.isVerified == true);
            userRequest.eMail = verifiedMailAddress?.address ?? "";
            userRequest.reference = request.Username!;

            var migrateResult = await userService.SaveUser(userRequest);

            var amorphieUserResult = await userService.GetUserByReference(request.Username);
            var amorphieUser = amorphieUserResult.Response;

            variables.wrongCredentials = false;

            variables.userInfoSerialized = JsonSerializer.Serialize(userInfo);
            variables.userSerialized = JsonSerializer.Serialize(amorphieUser);

            await ibContext.SaveChangesAsync();
            return Results.Ok(variables);
        }
    }
}