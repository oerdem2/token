using System.Dynamic;
using System.Text.Json;
using amorphie.token.core.Models.InternetBanking;
using amorphie.token.data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace amorphie.token.Modules.Login
{
    public static class SetNewSecurityQuestion
    {
        public static async Task<IResult> setNewSecurityQuestion(
        [FromBody] dynamic body,
        [FromServices] IbDatabaseContext ibContext
        )
        {
            await Task.CompletedTask;

            PasswordHasher passwordHasher = new();

            var ibUserSerialized = body.GetProperty("ibUserSerialized").ToString();
            IBUser ibUser = JsonSerializer.Deserialize<IBUser>(ibUserSerialized);

            var securityQuestionId = body.GetProperty("TRXamorphiemobileloginsetnewsecurityquestion").GetProperty("Data").GetProperty("entityData").GetProperty("questionId").ToString();
            var answer = body.GetProperty("TRXamorphiemobileloginsetnewsecurityquestion").GetProperty("Data").GetProperty("entityData").GetProperty("answer").ToString();
            var instanceId = body.GetProperty("InstanceId").ToString();

            var questionId = Guid.NewGuid();
            var securityQuestion = new IBQuestion()
            {
                Id = questionId,
                UserId = ibUser.Id,
                DefinitionId = Guid.Parse(securityQuestionId),
                EncryptedAnswer = passwordHasher.EncryptString(answer.Trim(), questionId.ToString("N")),
                CreatedByInstanceId = Guid.Parse(instanceId),
                CreatedByInstanceState = "SetNewSecurityQuestion",
                Status = 10,
                LastVerificationDate = DateTime.Now
            };
            await ibContext.Question.AddAsync(securityQuestion);
            await ibContext.SaveChangesAsync();

            dynamic variables = new ExpandoObject();
            variables.status = true;

            return Results.Ok(variables);
        }
    }
}