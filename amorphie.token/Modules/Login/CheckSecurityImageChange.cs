using System.Dynamic;
using System.Text.Json;
using amorphie.token.core.Models.InternetBanking;
using amorphie.token.data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace amorphie.token.Modules.Login
{
    public static class CheckSecurityImageChange
    {
        public static async Task<IResult> checkSecurityImageChange(
        [FromBody] dynamic body,
        [FromServices] IbDatabaseContext ibContext
        )
        {
            await Task.CompletedTask;

            var transitionName = body.GetProperty("LastTransition").ToString();

            var dataBody = body.GetProperty($"TRX-{transitionName}").GetProperty("Data");

            dynamic dataChanged = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(dataBody.ToString());

            dynamic targetObject = new System.Dynamic.ExpandoObject();

            targetObject.Data = dataChanged;

            var ibUserSerialized = body.GetProperty("ibUserSerialized").ToString();
            IBUser ibUser = JsonSerializer.Deserialize<IBUser>(ibUserSerialized);

            var securityImage = await ibContext.SecurityImage.Where(i => i.UserId == ibUser.Id)
                .OrderByDescending(i => i.CreatedAt).FirstOrDefaultAsync();

            dynamic variables = new Dictionary<string, dynamic>();
            if(securityImage == null || securityImage.RequireChange == true)
            {
                var securityImages = await ibContext.SecurityImageDefinition.Where(i => i.IsActive).Select(
                    i => new{
                        Id = i.Id,
                        ImagePath = i.ImagePath,
                        IsSelected = false,
                        EnTitle = i.TitleEn,
                        TrTitle = i.TitleTr
                    }
                ).ToListAsync();
                dataChanged.additionalData = new ExpandoObject();
                dataChanged.additionalData.securityImages = securityImages;
                targetObject.Data = dataChanged;
                targetObject.TriggeredBy = Guid.Parse(body.GetProperty($"TRX-{transitionName}").GetProperty("TriggeredBy").ToString());
                targetObject.TriggeredByBehalfOf = Guid.Parse(body.GetProperty($"TRX-{transitionName}").GetProperty("TriggeredByBehalfOf").ToString());
                variables.Add($"TRX{transitionName.ToString().Replace("-","")}", targetObject);
                variables.Add("status",true);
                variables.Add("changeSecurityImage",true);
            }
            else
            {
                variables.Add("status",true);
                variables.Add("changeSecurityImage",false);
            }
            
            return Results.Ok(variables);
        }
    }
}