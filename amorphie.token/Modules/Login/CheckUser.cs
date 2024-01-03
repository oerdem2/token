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

            var userResponse = await internetBankingUserService.GetUser(request.Username!);
            if(userResponse.StatusCode != 200)
            {
                dynamic variables = new ExpandoObject();
                variables.status = false;
                variables.message = "User Not Found";
                variables.LastTransition = "amorphie-login-error";
                return Results.Ok(variables);
            }
            var user = userResponse.Response;

            
            if(userResponse.StatusCode != 200)
            {
                dynamic variables = new ExpandoObject();
                variables.status = false;
                variables.message = "Username or password doesn't match";
                variables.LastTransition = "amorphie-login-error";
                return Results.Ok(variables);
            }
            var passwordResponse = await internetBankingUserService.GetPassword(user!.Id);
            var passwordRecord = passwordResponse.Response;
            
            var isVerified = internetBankingUserService.VerifyPassword(passwordRecord!.HashedPassword!,request.Password!,passwordRecord.Id.ToString());
            //Consider SuccessRehashNeeded
            if(isVerified != PasswordVerificationResult.Success)
            {
                dynamic variables = new ExpandoObject();
                variables.status = false;
                variables.message = "Username or password doesn't match";
                variables.PasswordTryCount = passwordRecord.AccessFailedCount;
                await ibContext.Password.Where(p => p.Id == passwordRecord.Id).ExecuteUpdateAsync(setters => setters.SetProperty(p => p.AccessFailedCount,p => p.AccessFailedCount + 1));

                return Results.Ok(variables);
            }
            else
            {
                await ibContext.Password.Where(p => p.Id == passwordRecord.Id).ExecuteUpdateAsync(setters => setters.SetProperty(p => p.AccessFailedCount,0));
                
            }
            var userInfoResult = await profileService.GetCustomerSimpleProfile(request.Username!);
            if(userInfoResult.StatusCode != 200)
            {
                dynamic variables = new ExpandoObject();
                variables.status = false;
                variables.message = "UserInfo Not Found";
                variables.PasswordTryCount = passwordRecord.AccessFailedCount;
                return Results.Ok(variables);
            }

            var userInfo = userInfoResult.Response;

            if(userInfo!.data!.profile!.Equals("customer") || !userInfo!.data!.profile!.status!.Equals("active"))
            {
                dynamic variables = new ExpandoObject();
                variables.status = false;
                variables.message = "User is Not Customer Or Not Active";
                variables.LastTransition = "amorphie-login-error";
                variables.PasswordTryCount = passwordRecord.AccessFailedCount;
                return Results.Ok(variables);
            }

            var mobilePhoneCount = userInfo!.data!.phones!.Count(p => p.type!.Equals("mobile"));
            if(mobilePhoneCount != 1)
            {
                dynamic variables = new ExpandoObject();
                variables.status = false;
                variables.message = "Bad Phone Data";
                variables.LastTransition = "amorphie-login-error";
                variables.PasswordTryCount = passwordRecord.AccessFailedCount;
                return Results.Ok(variables);
            }

            var mobilePhone = userInfo!.data!.phones!.FirstOrDefault(p => p.type!.Equals("mobile"));
            if(string.IsNullOrWhiteSpace(mobilePhone!.prefix) || string.IsNullOrWhiteSpace(mobilePhone!.number))
            {
                dynamic variables = new ExpandoObject();
                variables.status = false;
                variables.message = "Bad Phone Format";
                variables.LastTransition = "amorphie-login-error";
                variables.PasswordTryCount = passwordRecord.AccessFailedCount;
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

            var amorphieUserResult = await userService.Login(new LoginRequest(){Reference=request.Username!,Password=request.Password!});
            var amorphieUser = amorphieUserResult.Response;

            dynamic variablesSuccess = new ExpandoObject();
            variablesSuccess.status = true;
            variablesSuccess.PasswordTryCount = passwordRecord.AccessFailedCount;
            variablesSuccess.ibUserSerialized = JsonSerializer.Serialize(user);
            variablesSuccess.userInfoSerialized = JsonSerializer.Serialize(userInfo);
            variablesSuccess.userSerialized = JsonSerializer.Serialize(amorphieUser);
            variablesSuccess.passwordSerialized = JsonSerializer.Serialize(passwordRecord);
            return Results.Ok(variablesSuccess);
        }
    }
}