using System.Dynamic;
using amorphie.token.core;
using amorphie.token.data;
using Microsoft.AspNetCore.Mvc;

namespace amorphie.token;

public static class EkcyFaceCheck
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public static async Task<IResult> Check(
         [FromBody] dynamic body,
         [FromServices] IEkycService ekycService
     )
    {

        var transitionName = body.GetProperty("LastTransition").ToString();
        // var transactionId = body.GetProperty("InstanceId").ToString();
        var dataBody = body.GetProperty($"TRX-{transitionName}").GetProperty("Data");

        dynamic dataChanged = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(dataBody.ToString());


        var faceIsSuccess = dataChanged.entityData.IsSuccess;
        dynamic variables = new Dictionary<string, dynamic>();
        dataChanged.additionalData = new ExpandoObject();



        bool faceStatus = false;
        bool showHomePageButton = true;
        bool showTyrAgainButton = true;
        var sessionId = body.GetProperty("SessionId").ToString();
        if (faceIsSuccess && !String.IsNullOrEmpty(sessionId))
        {
            
            var session = await ekycService.GetSessionInfoAsync(Guid.Parse(sessionId));
            if (session.Data is not null && session.Data.Face is not null && session.Data.Face.ValidityLevel > 0)
            {
                dataChanged.additionalData.isEkyc = true;// gitmek istediği data 
                dataChanged.additionalData.FaceReadSuccess = true;
                
                faceStatus = true;
            }

        }

        var faceCurrentFailedCount = Convert.ToInt32(body.GetProperty("CurrentFaceFailedCount").ToString());
        if(!faceStatus){
            if (faceCurrentFailedCount >= EkycConstants.FaceFailedMaxTryCount)
            {
                showTyrAgainButton = false;
            }
            faceCurrentFailedCount++;
        }

        dataChanged.additionalData.ShowHomePageButton = showHomePageButton;
        dataChanged.additionalData.ShowTryAgainButton = showTyrAgainButton;

        variables.Add("Init", true);
        variables.Add("IsSelfServiceAvaliable", true);
        variables.Add("CurrentFaceFailedCount", faceCurrentFailedCount);
        
        return Results.Ok(variables);

        // return Task.FromResult(Results.Ok("data"));
    }
}
