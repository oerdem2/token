using System.Dynamic;
using amorphie.token.core;
using Microsoft.AspNetCore.Mvc;

namespace amorphie.token;

public class EkycStatusCheck
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public static async Task<IResult> Check(
       [FromBody] dynamic body,
       [FromServices] IEkycService ekycService,
       IConfiguration configuration
   )
    {

        var transitionName = body.GetProperty("LastTransition").ToString();

        // var transactionId = body.GetProperty("InstanceId").ToString();
        var dataBody = body.GetProperty($"TRX-{transitionName}").GetProperty("Data");
        dynamic dataChanged = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(dataBody.ToString());
         dynamic targetObject = new System.Dynamic.ExpandoObject();
        targetObject.Data = dataChanged;

        dynamic variables = new Dictionary<string, dynamic>();
        dataChanged.additionalData = new ExpandoObject();

        var citizenShipNumber = body.GetProperty("UserName").ToString();
        var callType = body.GetProperty("CallType").ToString();

        var isSuccess = dataChanged.entityData.IsSuccess;
        if(isSuccess){
            dataChanged.additionalData.pages = new List<EkycPageModel>
                {
                    EkycAdditionalDataContstants.StandartItem,

                };
        }

        var videoCallCheck = false;
        var callTransactionType = "";

        if (callType == EkycCallTypeConstants.Mevduat_ON || callType == EkycCallTypeConstants.Mevduat_HEPSIBURADA)
        {
            // Get token
            var request = new EkycMevduatStatusCheckModels.Request()
            {
                CallType = callType,
                CitizenshipNumber = Convert.ToInt64(citizenShipNumber),
                Counter = 1
            };
            var statusCheck = await ekycService.CheckCallStatusForMevduat(request);
            if(statusCheck.StatusCode==200){
                videoCallCheck = true;

                switch(statusCheck.Response.CallTransactionsType){
                    case EkycMevduatStatusCheckModels.EkycPostCallTransactionsType.Survey:
                        callTransactionType = "Survey";
                        break;
                    case EkycMevduatStatusCheckModels.EkycPostCallTransactionsType.CreditUsage:
                        callTransactionType = "CreditUsage";
                        break;
                    case EkycMevduatStatusCheckModels.EkycPostCallTransactionsType.SurveyAndCreditUsage:
                        callTransactionType = "SurveyAndCreditUsage";
                        break;
                    case EkycMevduatStatusCheckModels.EkycPostCallTransactionsType.None:
                        callTransactionType = "None";
                        break;
                }


            }
            
             
        }

        
        variables.Add("EkycButton",callTransactionType);
        variables.Add("Init", true);
        variables.Add("VideoCallCheck", videoCallCheck);


         targetObject.TriggeredBy = Guid.Parse(body.GetProperty($"TRX-{transitionName}").GetProperty("TriggeredBy").ToString());
        targetObject.TriggeredByBehalfOf = Guid.Parse(body.GetProperty($"TRX-{transitionName}").GetProperty("TriggeredByBehalfOf").ToString());
        variables.Add($"TRX{transitionName.ToString().Replace("-", "")}", targetObject);


        return Results.Ok(variables);

    }
}
