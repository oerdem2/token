using System.Dynamic;
using amorphie.token.data;
using Microsoft.AspNetCore.Mvc;

namespace amorphie.token;

public static  class EkycOcrCheck
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public static async Task<IResult> Check(
        [FromBody] dynamic body,
        [FromServices] IbDatabaseContext ibDatabaseContext        
    ){
        // var transitionName = body.GetProperty("LastTransition").ToString();
        // var dataBody = body.GetProperty($"TRX-{transitionName}").GetProperty("Data");
        // var requestBodySerialized = body.GetProperty("requestBody").ToString();,
        dynamic variables = new ExpandoObject();
        variables.Init = true;
        variables.OcrStatus  = true;
        return Results.Ok(variables);

        // return Task.FromResult(Results.Ok("data"));
    }
}
