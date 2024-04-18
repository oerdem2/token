using System.Dynamic;
using System.Text.Json;
using amorphie.token.core.Models.InternetBanking;
using amorphie.token.data;
using amorphie.token.Services.Migration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace amorphie.token.Modules.Login
{
    public static class SetNewSecurityQuestion
    {
        [ApiExplorerSettings(IgnoreApi = true)]
        public static async Task<IResult> setNewSecurityQuestion(
        [FromBody] dynamic body,
        [FromServices] IbDatabaseContext ibContext,
        [FromServices] IMigrationService migrationService
        )
        {
            await Task.CompletedTask;

            PasswordHasher passwordHasher = new();

            var ibUserSerialized = body.GetProperty("ibUserSerialized").ToString();
            IBUser ibUser = JsonSerializer.Deserialize<IBUser>(ibUserSerialized);
            
            var amorphieUserSerialized = body.GetProperty("userSerialized").ToString();
            LoginResponse amorphieUser = JsonSerializer.Deserialize<LoginResponse>(amorphieUserSerialized);

            var transitionName = body.GetProperty("LastTransition").ToString();
            var securityQuestionId = body.GetProperty("TRX-" + transitionName).GetProperty("Data").GetProperty(WorkflowConstants.ENTITY_DATA_FIELD).GetProperty("questionId").ToString();
            var answer = body.GetProperty("TRXamorphiemobileloginsetnewsecurityquestion").GetProperty("Data").GetProperty(WorkflowConstants.ENTITY_DATA_FIELD).GetProperty("answer").ToString();
            var instanceId = body.GetProperty("InstanceId").ToString();

            var questionId = Guid.NewGuid();
            var securityQuestion = new IBQuestion()
            {
                Id = questionId,
                UserId = ibUser.Id,
                DefinitionId = Guid.Parse(securityQuestionId),
                EncryptedAnswer = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT").Equals("Prod") ? passwordHasher.EncryptString(answer.Trim(), questionId.ToString("N")) : answer.Trim(),
                CreatedByInstanceId = Guid.Parse(instanceId),
                CreatedByInstanceState = "SetNewSecurityQuestion",
                Status = 10,
                LastVerificationDate = DateTime.Now
            };
            await ibContext.Question.AddAsync(securityQuestion);
            await ibContext.SaveChangesAsync();

            var migrateUserInfoResult = await migrationService.MigrateUserData(amorphieUser.Id,ibUser.Id);
            
            dynamic variables = new ExpandoObject();
            variables.status = true;

            return Results.Ok(variables);
        }
    }
}