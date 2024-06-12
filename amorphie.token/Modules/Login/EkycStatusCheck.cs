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
        if (isSuccess)
        {
            dataChanged.additionalData.pages = new List<EkycPageModel>
                {
                    EkycAdditionalDataContstants.StandartItem,

                };
                
        }

        var videoCallCheck = false;
        var callTransactionType = "";
        var callResult = EkycResultConstants.VideoCallCompleted;

        if (callType == EkycCallTypeConstants.Mevduat_ON ||
        callType == EkycCallTypeConstants.Mevduat_HEPSIBURADA ||
        callType == EkycCallTypeConstants.Mevduat_BRGN)
        {
            // Get token
            var request = new EkycMevduatStatusCheckModels.Request()
            {
                CallType = callType,
                CitizenshipNumber = Convert.ToInt64(citizenShipNumber),
                Counter = 1
            };
            var statusCheck = await ekycService.CheckCallStatusForMevduat(request);
            
            if (statusCheck.StatusCode == 200)
            {
                videoCallCheck = true;

                callTransactionType = statusCheck?.Response?.CallTransactionsType switch
                {
                    EkycMevduatStatusCheckModels.EkycPostCallTransactionsType.Survey => "Survey",
                    EkycMevduatStatusCheckModels.EkycPostCallTransactionsType.CreditUsage => "CreditUsage",
                    EkycMevduatStatusCheckModels.EkycPostCallTransactionsType.SurveyAndCreditUsage => "SurveyAndCreditUsage",
                    _ => "None"
                };

                switch (statusCheck?.Response?.ReferenceType)
                {

                    case EkycMevduatStatusCheckModels.EkycProcessRedirectType.Continue:
                        callResult = EkycResultConstants.VideoCallCompleted;
                        break;
                    case EkycMevduatStatusCheckModels.EkycProcessRedirectType.Exit:
                        callResult = EkycResultConstants.VideoCallExit;
                        break;
                    case EkycMevduatStatusCheckModels.EkycProcessRedirectType.ToCourier:
                        callResult = EkycResultConstants.VideoCallCompleted;
                        break;
                    case EkycMevduatStatusCheckModels.EkycProcessRedirectType.Cancel:
                        callResult = EkycResultConstants.VideoCallFailed;
                        videoCallCheck = false;
                        break;
                    case EkycMevduatStatusCheckModels.EkycProcessRedirectType.Retry:
                        callResult = EkycResultConstants.VideoCallSuccess;
                        break;
                    default :
                        callResult = EkycResultConstants.VideoCallCompleted;
                        break;
                    

                }



            }


        }
        dataChanged.additionalData.exitTransition = "amorphie-ekyc-exit";
        variables.Add("EkycResult",callResult);
        variables.Add("EkycButton", callTransactionType);
        variables.Add("Init", true);
        variables.Add("VideoCallCheck", videoCallCheck);


        targetObject.TriggeredBy = Guid.Parse(body.GetProperty($"TRX-{transitionName}").GetProperty("TriggeredBy").ToString());
        targetObject.TriggeredByBehalfOf = Guid.Parse(body.GetProperty($"TRX-{transitionName}").GetProperty("TriggeredByBehalfOf").ToString());
        variables.Add($"TRX{transitionName.ToString().Replace("-", "")}", targetObject);


        return Results.Ok(variables);

    }



    [ApiExplorerSettings(IgnoreApi = true)]
    public static async Task<IResult> CheckForNonDeposit(
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

        
        var hasProperty = body.GetType().GetProperty("StatusCheckTimerResult")!=null;
        var hasReason = dataChanged.entityData.GetType().GetProperty("Reason") != null;
        string ekycResult = EkycResultConstants.VideoCallCompleted;



        if(hasReason){
            var reason = dataChanged.entityData.Reason;
            if(reason!=null && reason!="10"){
                ekycResult = EkycResultConstants.VideoCallFailed;
            }
        }

        if(hasProperty){
            // Sor !
            ekycResult = EkycResultConstants.VideoCallExit;
        }

       


        
        

       
        dataChanged.additionalData.exitTransition = "amorphie-ekyc-exit";
        // variables.Add("EkycResult",callResult);
        // variables.Add("EkycButton", callTransactionType);
        variables.Add("Init", true);
        variables.Add("EkycResult",ekycResult);
        variables.Add("EkycCallType", callType);


        targetObject.TriggeredBy = Guid.Parse(body.GetProperty($"TRX-{transitionName}").GetProperty("TriggeredBy").ToString());
        targetObject.TriggeredByBehalfOf = Guid.Parse(body.GetProperty($"TRX-{transitionName}").GetProperty("TriggeredByBehalfOf").ToString());
        variables.Add($"TRX{transitionName.ToString().Replace("-", "")}", targetObject);


        return Results.Ok(variables);

    }
}
