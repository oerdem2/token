
using System.Dynamic;
using System.Text.Json;
using amorphie.token.core.Models.InternetBanking;
using amorphie.token.data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace amorphie.token.Modules;

public static class GenerateResetPasswordQuestion
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public static async Task<IResult> generateResetPasswordQuestion(
    [FromBody] dynamic body,
    IbDatabaseContext ibContext
    )
    {
        dynamic variables = new ExpandoObject();

        var ibUserSerialized = body.GetProperty("ibUserSerialized").ToString();
        IBUser ibUser = JsonSerializer.Deserialize<IBUser>(ibUserSerialized);

        var securityQuestion = await ibContext.Question.Where(q => q.UserId == ibUser.Id)
                .OrderByDescending(q => q.CreatedAt).FirstOrDefaultAsync();
        
        PasswordHasher passwordHasher = new();
        var answer =  passwordHasher.DecryptString(securityQuestion.EncryptedAnswer,securityQuestion.Id.ToString("N")).Trim();

        if(answer.Length == 2)
        {
            variables.answerFirstCharIndex = 1;
            variables.answerSecondCharIndex = 2;
        }
        else
        {
            var rnd = new Random();
            int seperator = answer.Length / 2;
            variables.answerFirstCharIndex = rnd.Next(1,seperator);
            variables.answerSecondCharIndex = rnd.Next(seperator,answer.Length+1);
        }
        

        return Results.Ok(variables);
    }


}
