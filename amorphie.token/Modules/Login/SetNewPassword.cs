using System.Dynamic;
using System.Text.Json;
using amorphie.core.Zeebe.dapr;
using amorphie.token.core.Models.InternetBanking;
using amorphie.token.data;
using amorphie.token.Services.InternetBanking;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace amorphie.token.Modules.Login
{
    public static class SetNewPassword
    {
        [ApiExplorerSettings(IgnoreApi = true)]
        public static async Task<IResult> setNewPassword(
        [FromBody] dynamic body,
        [FromServices] IbDatabaseContext ibContext,
        [FromServices] IInternetBankingUserService internetBankingUserService
        )
        {
            var transitionName = body.GetProperty("LastTransition").ToString();
            var newPassword = body.GetProperty("TRX-" + transitionName).GetProperty("Data").GetProperty("entityData").GetProperty("newPassword").ToString();

            var ibUserSerialized = body.GetProperty("ibUserSerialized").ToString();
            IBUser ibUser = JsonSerializer.Deserialize<IBUser>(ibUserSerialized);
            var oldPasswords = await ibContext.Password.Where(p => p.UserId == ibUser.Id).OrderByDescending(p => p.CreatedAt).Take(5).ToListAsync();

            PasswordHasher passwordHasher = new();
            IBPassword password = new IBPassword
            {
                AccessFailedCount = 0,
                CreatedByUserName = "Amorphie",
                UserId = ibUser.Id
            };

            dynamic variables = new ExpandoObject();
            foreach (var pass in oldPasswords)
            {
                if (passwordHasher.VerifyHashedPassword(pass.HashedPassword, newPassword, pass.Id.ToString()) != PasswordVerificationResult.Failed)
                {
                    variables.status = false;
                    variables.message = "New Password Can Not Be Same With Last 5 Passwords";
                    return Results.Ok(variables);
                }
            }

            password.HashedPassword = passwordHasher.HashPassword(newPassword, password.Id.ToString());
            await ibContext.Password.AddAsync(password);

            try
            {
                //Check Process is Remember Password or Not
                var isValidated = body.GetProperty("isValidated");
                var securityImage = await ibContext.SecurityImage.Where(i => i.UserId == ibUser.Id)
                .OrderByDescending(i => i.CreatedAt).FirstOrDefaultAsync();
                securityImage.RequireChange = true;
            }
            catch (Exception)
            {

            }

            await ibContext.SaveChangesAsync();
            variables.status = true;
            return Results.Ok(variables);
        }
    }
}