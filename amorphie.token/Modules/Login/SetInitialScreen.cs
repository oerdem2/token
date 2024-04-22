using System.Dynamic;
using System.Text.Json;
using amorphie.token.core.Models.InternetBanking;
using amorphie.token.data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace amorphie.token.Modules.Login
{
    public static class SetInitialScreen
    {
        [ApiExplorerSettings(IgnoreApi = true)]
        public static async Task<IResult> checkInitialScreens(
        [FromBody] dynamic body,
        [FromServices] IbDatabaseContext ibContext,
        [FromServices] IPasswordRememberService passwordRememberService

        )
        {
            // await Task.CompletedTask;

            var transitionName = body.GetProperty("LastTransition").ToString();

            var dataBody = body.GetProperty($"TRX-{transitionName}").GetProperty("Data");
            var requestBodySerialized = body.GetProperty("requestBody").ToString();
            TokenRequest request = JsonSerializer.Deserialize<TokenRequest>(requestBodySerialized);

            dynamic dataChanged = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(dataBody.ToString());

            dynamic targetObject = new System.Dynamic.ExpandoObject();

            targetObject.Data = dataChanged;

            var ibUserSerialized = body.GetProperty("ibUserSerialized").ToString();
            IBUser ibUser = JsonSerializer.Deserialize<IBUser>(ibUserSerialized);

            var securityQuestion = await ibContext.Question.Where(q => q.UserId == ibUser.Id)
                .OrderByDescending(q => q.CreatedAt).FirstOrDefaultAsync();


            dynamic variables = new Dictionary<string, dynamic>();
            var resultList = new List<string>(){
                "PAGE_IDENTITY_CARD"
            };
            if (securityQuestion == null)
            {
                variables.Add("status", true);
                variables.Add("changeSecurityQuestion", true);
            }
            else
            {
                var securityQuestionDefinition = await ibContext.QuestionDefinition.
                    FirstOrDefaultAsync(d => d.Id == securityQuestion.DefinitionId && d.IsActive && d.Type == 10);
                if (securityQuestionDefinition == null || securityQuestion?.Status != 10)
                {
                    variables.Add("status", true);

                }
                else
                {
                    variables.Add("status", true);
                    resultList.Add("PAGE_QUESTION");
                }
            }

            var hasCards = await passwordRememberService.HasCardAsync(request.Username);
            if (hasCards.Response)
            {
                resultList.Add("PAGE_CARD");
            }

            variables.Add("pages", resultList);




            return Results.Ok(variables);
        }
    }
}