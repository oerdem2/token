using System.Dynamic;
using System.Text.Json;
using amorphie.token.core.Models.InternetBanking;
using amorphie.token.data;
using Microsoft.AspNetCore.Mvc;

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

            PasswordHasher passwordHasher = new();
            IBPassword password = new IBPassword
            {
                AccessFailedCount = 0,
                CreatedByUserName = "Amorphie",
                UserId = ibUser.Id
            };
            password.HashedPassword = passwordHasher.HashPassword(newPassword,password.Id.ToString());

            await ibContext.Password.AddAsync(password);
            await ibContext.SaveChangesAsync();

            dynamic variables = new ExpandoObject();
            variables.status = true;
            return Results.Ok(variables);
        }
    }
}