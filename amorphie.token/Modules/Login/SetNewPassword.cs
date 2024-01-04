using System.Dynamic;
using System.Text.Json;
using amorphie.core.Zeebe.dapr;
using amorphie.token.core.Models.InternetBanking;
using amorphie.token.data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace amorphie.token.Modules.Login
{
    public static class SetNewPassword
    {
        public static async Task<IResult> setNewPassword(
        [FromBody] dynamic body,
        [FromServices] IbDatabaseContext ibContext
        )
        {
            await Task.CompletedTask;

            var newPassword = body.GetProperty("TRXamorphiemobileloginsetnewpassword").GetProperty("Data").GetProperty("entityData").GetProperty("newPassword").ToString();

            var ibUserSerialized = body.GetProperty("ibUserSerialized").ToString();
            IBUser ibUser = JsonSerializer.Deserialize<IBUser>(ibUserSerialized);

            var oldPasswords = await ibContext.Password.Where(p => p.UserId == ibUser.Id).OrderBy(p => p.CreatedAt).Take(5).ToListAsync();

            PasswordHasher passwordHasher = new();
            IBPassword password = new IBPassword
            {
                AccessFailedCount = 0,
                CreatedByUserName = "Amorphie",
                UserId = ibUser.Id
            };
            password.HashedPassword = passwordHasher.HashPassword(newPassword, password.Id.ToString());

            dynamic variables = new ExpandoObject();
            foreach (var pass in oldPasswords)
            {
                if (pass.HashedPassword?.Equals(password.HashedPassword) ?? false)
                {
                    variables.status = false;
                    variables.message = "New Password Can Not Be Same With Last 5 Passwords";
                    return Results.Ok(variables);
                }
            }

            await ibContext.Password.AddAsync(password);
            await ibContext.SaveChangesAsync();


            variables.status = true;
            return Results.Ok(variables);
        }
    }
}