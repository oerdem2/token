using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using amorphie.token.core.Models.InternetBanking;
using amorphie.token.data;
using Microsoft.AspNetCore.Mvc;

namespace amorphie.token.Modules.Login
{
    public static class CheckPasswordChange
    {
        [ApiExplorerSettings(IgnoreApi = true)]
        public static async Task<IResult> checkPasswordChange(
        [FromBody] dynamic body,
        [FromServices] IbDatabaseContext ibDatabaseContext
        )
        {
            await Task.CompletedTask;

            var passwordSerialized = body.GetProperty("passwordSerialized").ToString();
            IBPassword password = JsonSerializer.Deserialize<IBPassword>(passwordSerialized);

            dynamic variables = new ExpandoObject();
            var mustResetPassword = password.MustResetPassword ?? false;

            if (DateTime.Now > password.CreatedAt.AddDays(90) || mustResetPassword)
            {

                variables.status = true;
                variables.changePassword = true;
                return Results.Ok(variables);
            }
            else
            {
                variables.changePassword = false;
                variables.status = true;
            }


            return Results.Ok(variables);
        }
    }
}