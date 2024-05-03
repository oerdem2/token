using System.Dynamic;
using amorphie.token.core;
using amorphie.token.data;
using Microsoft.AspNetCore.Mvc;

namespace amorphie.token;

public static class EkycNfcCheck
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


        var nfcIsSuccess = dataChanged.entityData.IsSuccess;
        dynamic variables = new Dictionary<string, dynamic>();
        dataChanged.additionalData = new ExpandoObject();


        bool identityNoCompatible = false;
        bool nfcStatus = false;
        bool showHomePageButton = true;
        bool showTyrAgainButton = true;

        var sessionId = body.GetProperty("SessionId").ToString();
        if (nfcIsSuccess && !String.IsNullOrEmpty(sessionId))
        {
            
            var session = await ekycService.GetSessionInfoAsync(Guid.Parse(sessionId));
            if (session is not null && session.Data is not null && session.Data?.IDChip is not null)
            {
                var identityNo = body.GetProperty("UserName").ToString();

                identityNoCompatible = session.Data.IDChip.IdentityNo == identityNo;
                if (identityNoCompatible && session.Data.IDChip.IsValid is true)
                {
                    dataChanged.additionalData.isEkyc = true;// gitmek istediği data 
                    dataChanged.additionalData.NfcReadSuccess = true;
                    nfcStatus = true;
                }

            }

        }

        var nfcCurrentFailedCount = Convert.ToInt32(body.GetProperty("CurrentNfcFailedCount").ToString());


        if (!nfcStatus)
        {

            if (nfcCurrentFailedCount >= EkycConstants.NfcFailedMaxTryCount)
            {
                showTyrAgainButton = false;
            }
            nfcCurrentFailedCount++;
        }

        dataChanged.additionalData.ShowHomePageButton = showHomePageButton;
        dataChanged.additionalData.ShowTryAgainButton = showTyrAgainButton;


        variables.Add("Init", true);
        variables.Add("NfcStatus", true);
        variables.Add("CurrentNfcFailedCount", nfcCurrentFailedCount);
        return Results.Ok(variables);

        // return Task.FromResult(Results.Ok("data"));
    }
}
