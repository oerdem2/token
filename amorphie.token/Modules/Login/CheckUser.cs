using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using amorphie.token.data;
using amorphie.token.Services.InternetBanking;
using amorphie.token.Services.Profile;
using Azure.Core.Pipeline;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace amorphie.token.Modules.Login
{
    public static class CheckUser
    {
        public static async Task<IResult> checkUser(
        [FromBody] dynamic body,
        [FromServices] IInternetBankingUserService internetBankingUserService,
        [FromServices] IProfileService profileService,
        [FromServices] IUserService userService,
        [FromServices] IbDatabaseContext ibContext
        )
        {
            await Task.CompletedTask;
            var requestBodySerialized = body.GetProperty($"TRXamorphiemobilelogin").GetProperty("Data").GetProperty("entityData").ToString();
            TokenRequest request = JsonSerializer.Deserialize<TokenRequest>(requestBodySerialized);

            dynamic variables = new ExpandoObject();
            variables.PasswordTryCount = 0;
            variables.wrongCredentials = false;

            var userResponse = await internetBankingUserService.GetUser(request.Username!);
            if (userResponse.StatusCode != 200)
            {
                variables.status = false;
                variables.message = "User Not Found";
                variables.LastTransition = "amorphie-login-error";
                return Results.Ok(variables);
            }
            var user = userResponse.Response;
            variables.ibUserSerialized = JsonSerializer.Serialize(user);

            var userStatus = await ibContext.Status.Where(s => !s.State.HasValue || s.State.Value == 10).OrderByDescending(s => s.CreatedAt).FirstOrDefaultAsync();
            if (userStatus?.Type == 30 || userStatus?.Type == 40)
            {
                variables.status = false;
                variables.message = "User Not Active";
                variables.LastTransition = "amorphie-login-error";
                return Results.Ok(variables);
            }

            var passwordResponse = await internetBankingUserService.GetPassword(user!.Id);
            if (passwordResponse.StatusCode != 200)
            {
                variables.status = false;
                variables.message = "Password not found";
                variables.LastTransition = "amorphie-login-error";
                return Results.Ok(variables);
            }
            var passwordRecord = passwordResponse.Response;

            var isVerified = internetBankingUserService.VerifyPassword(passwordRecord!.HashedPassword!, request.Password!, passwordRecord.Id.ToString());
            //Consider SuccessRehashNeeded
            if (isVerified != PasswordVerificationResult.Success)
            {
                variables.status = false;
                variables.message = "Username or password doesn't match";
                passwordRecord.AccessFailedCount = (passwordRecord.AccessFailedCount ?? 0 )+ 1;
                variables.wrongCredentials = true;
                if(passwordRecord.AccessFailedCount >= 5)
                {
                    variables.disableUser = true;
                }
                else
                {
                    variables.disableUser = false;
                }
                return Results.Ok(variables);
            }
            else
            {
                passwordRecord.AccessFailedCount = 0;
            }

            variables.PasswordTryCount = passwordRecord.AccessFailedCount;

            var userInfoResult = await profileService.GetCustomerSimpleProfile(request.Username!);
            if (userInfoResult.StatusCode != 200)
            {
                variables.status = false;
                variables.message = "UserInfo Not Found";
                return Results.Ok(variables);
            }

            var userInfo = userInfoResult.Response;

            if (userInfo!.data!.profile!.Equals("customer") || !userInfo!.data!.profile!.status!.Equals("active"))
            {
                variables.status = false;
                variables.message = "User is Not Customer Or Not Active";
                variables.LastTransition = "amorphie-login-error";
                return Results.Ok(variables);
            }

            var mobilePhoneCount = userInfo!.data!.phones!.Count(p => p.type!.Equals("mobile"));
            if (mobilePhoneCount != 1)
            {
                variables.status = false;
                variables.message = "Bad Phone Data";
                variables.LastTransition = "amorphie-login-error";
                
                return Results.Ok(variables);
            }

            var mobilePhone = userInfo!.data!.phones!.FirstOrDefault(p => p.type!.Equals("mobile"));
            if (string.IsNullOrWhiteSpace(mobilePhone!.prefix) || string.IsNullOrWhiteSpace(mobilePhone!.number))
            {
                variables.status = false;
                variables.message = "Bad Phone Format";
                variables.LastTransition = "amorphie-login-error";
                return Results.Ok(variables);
            }

            var userRequest = new UserInfo
            {
                firstName = userInfo!.data.profile!.name!,
                lastName = userInfo!.data.profile!.name!,
                phone = new core.Models.User.UserPhone()
                {
                    countryCode = mobilePhone!.countryCode!,
                    prefix = mobilePhone!.prefix,
                    number = mobilePhone!.number
                },
                state = "Active",
                salt = passwordRecord.Id.ToString(),
                password = request.Password!,
                explanation = "Migrated From IB",
                reason = "Amorphie Login",
                isArgonHash = true
            };

            var verifiedMailAddress = userInfo.data.emails!.FirstOrDefault(m => m.isVerified == true);
            userRequest.eMail = verifiedMailAddress?.address ?? "";
            userRequest.reference = request.Username!;

            var migrateResult = await userService.SaveUser(userRequest);

            var amorphieUserResult = await userService.Login(new LoginRequest() { Reference = request.Username!, Password = request.Password! });
            var amorphieUser = amorphieUserResult.Response;

            variables.status = true;
            
            variables.userInfoSerialized = JsonSerializer.Serialize(userInfo);
            variables.userSerialized = JsonSerializer.Serialize(amorphieUser);
            variables.passwordSerialized = JsonSerializer.Serialize(passwordRecord);

            await ibContext.SaveChangesAsync();
            return Results.Ok(variables);
        }
    }
}