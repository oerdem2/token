using System.Dynamic;
using System.Text.Json;
using amorphie.token.core.Models.InternetBanking;
using amorphie.token.data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace amorphie.token.Modules.Login
{
    public static class SetNewSecurityImage
    {
        [ApiExplorerSettings(IgnoreApi = true)]
        public static async Task<IResult> setNewSecurityImage(
        [FromBody] dynamic body,
        [FromServices] IbDatabaseContext ibContext
        )
        {

            var ibUserSerialized = body.GetProperty("ibUserSerialized").ToString();
            IBUser ibUser = JsonSerializer.Deserialize<IBUser>(ibUserSerialized);

            var transitionName = body.GetProperty("LastTransition").ToString();
            var securityImageId = body.GetProperty("TRX-"+transitionName).GetProperty("Data").GetProperty("entityData").GetProperty("imageId").ToString();
            var instanceId = body.GetProperty("InstanceId").ToString();

            var securityImage = new IBSecurityImage()
            {
                RequireChange = null,
                UserId = ibUser.Id,
                DefinitionId = Guid.Parse(securityImageId),
                CreatedByInstanceId = Guid.Parse(instanceId),
                CreatedByInstanceState = "SetNewSecurityImage"

            };
            await ibContext.SecurityImage.AddAsync(securityImage);
            await ibContext.SaveChangesAsync();

            dynamic variables = new ExpandoObject();
            variables.status = true;

            return Results.Ok(variables);
        }
    }
}