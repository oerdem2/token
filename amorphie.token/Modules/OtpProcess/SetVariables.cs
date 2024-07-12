
using System.Dynamic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace amorphie.token.Modules.OtpProcess
{
    public static class SetVariables
    {
        [ApiExplorerSettings(IgnoreApi = true)]
        public static async Task<IResult> setVariables(
        [FromBody] dynamic body,
        IConfiguration configuration
        )
        {
            string transitionName = body.GetProperty("LastTransition").ToString();
            var entityData = body.GetProperty("TRX" + transitionName.Replace("-","")).GetProperty("Data").GetProperty("entityData");
            var otpProcessData = entityData.GetProperty("AmorphieOtpProcessRequest");
            
            dynamic variables = new ExpandoObject();
            variables.AmorphieOtpProcessRequest = otpProcessData;
            return Results.Ok(variables);
        }
    }
}