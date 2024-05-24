using amorphie.token.core;
using Microsoft.AspNetCore.Mvc;
using System.Dynamic;
using System.Runtime.CompilerServices;
namespace amorphie.token;

public static class EkycPrepare
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public static async Task<IResult> Prepare(
        [FromBody] dynamic body,
        [FromServices] IEkycService ekycService,
        IConfiguration configuration

    )
    {
        var transitionName = body.GetProperty("LastTransition").ToString();
        var transactionId = body.GetProperty("InstanceId").ToString();
        var dataBody = body.GetProperty($"TRX-{transitionName}").GetProperty("Data");

        dynamic dataChanged = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(dataBody.ToString());


        dynamic targetObject = new System.Dynamic.ExpandoObject();
        targetObject.Data = dataChanged;
        var citizenShipNumber = dataChanged.entityData.UserName;
        var callType = dataChanged.entityData.CallType;
        var wfId = dataChanged.entityData.WfId;
        var constCallType = ekycService.GetCallType(callType);
        var instance = Guid.NewGuid();
        var isSelfServiceAvaible = true;
        if (constCallType == EkycCallTypeConstants.Mevduat_ON || callType == EkycCallTypeConstants.Mevduat_HEPSIBURADA)
        {
            //var wfId = body.GetProperty("Wfi").ToString(); TODO: use this part test or prod.
            wfId = transactionId;
            Guid.TryParse(wfId, out instance);
            isSelfServiceAvaible = false;

        }
        else
        {

            Guid.TryParse(transactionId, out instance);
        }





        var registerResult = await ekycService.CreateSession(instance, citizenShipNumber, callType);

        //Register the enqura 



        dynamic variables = new Dictionary<string, dynamic>();


        // Set config variables :) 
        variables.Add("Init", true);
        variables.Add("IsSessionCreated", registerResult);
        variables.Add("CurrentOcrFailedCount", 0);
        variables.Add("CurrentNfcFailedCount", 0);
        variables.Add("CurrentFaceFailedCount", 0);
        variables.Add("CallType", constCallType);
        variables.Add("Name",registerResult.Name);
        variables.Add("Surname",registerResult.Surname);
        variables.Add("IsSelfServiceAvaible", isSelfServiceAvaible);
        variables.Add("Instance", instance);

        // Set failed count from body




        var ocrMinCount = Convert.ToInt32(configuration["EkycOcrFailMinTryCountDefault"]);
        var nfcMinCount = Convert.ToInt32(configuration["EkycNfcFailMinTryCountDefault"]);
        var faceMinCount = Convert.ToInt32(configuration["EkycFaceFailMinTryCountDefault"]);

        var ocrMaxCount = Convert.ToInt32(configuration["EkycOcrFailMaxTryCountDefault"]);
        var nfcMaxCount = Convert.ToInt32(configuration["EkycNfcFailMaxTryCountDefault"]);
        var faceMaxCount = Convert.ToInt32(configuration["EkycFaceFailMaxTryCountDefault"]);

        if (constCallType == EkycCallTypeConstants.Mevduat_ON || constCallType == EkycCallTypeConstants.Mevduat_BRGN)
        {
            ocrMinCount = Convert.ToInt32(configuration["EkycOcrFailMinTryCountMevduat"]);
            nfcMinCount = Convert.ToInt32(configuration["EkycNfcFailMinTryCountMevduat"]);
            faceMinCount = Convert.ToInt32(configuration["EkycFaceFailMinTryCountMevduat"]);

            ocrMaxCount = Convert.ToInt32(configuration["EkycOcrFailMaxTryCountMevduat"]);
            nfcMaxCount = Convert.ToInt32(configuration["EkycNfcFailMaxTryCountMevduat"]);
            faceMaxCount = Convert.ToInt32(configuration["EkycFaceFailMaxTryCountMevduat"]);
        }


        variables.Add("OcrFailedTryCount", ocrMinCount);
        variables.Add("NfcFailedTryCount", nfcMinCount);
        variables.Add("FaceFailedTryCount", faceMinCount);

        variables.Add("OcrFailedMaxTryCount", ocrMaxCount);
        variables.Add("NfcFailedMaxTryCount", nfcMaxCount);
        variables.Add("FaceFailedMaxTryCount", faceMaxCount);


        variables.Add("UserName", citizenShipNumber);


        dataChanged.additionalData = new ExpandoObject();
        dataChanged.additionalData.isEkyc = true;// gitmek istediği data 
        dataChanged.additionalData.callType = registerResult.CallType;
        dataChanged.additionalData.customerName = registerResult.Name; // bu kısımları doldur.
        dataChanged.additionalData.customerSurname = registerResult.Surname;
        dataChanged.additionalData.instanceId = instance;
        dataChanged.additionalData.pages = new List<EkycPageModel>{
            new EkycPageModel 
            { 
                type="waiting", 
                image="wait", 
                title="Kimlik Okuma Adımları Yükleniyor",
                navText = "Müşterimiz Ol",
                popUp= new EkycPopUpModel{
                    image="alert",
                    title = "Görüntülü Görüşmeyi Sonlandırmak İstediğinize Emin Misiniz?",
                    subTexts = new List<string>{"Görüntülü görüşme işleminiz sonlandırılacaktır, onaylıyor musunuz?"},
                    buttons = new List<EkycButtonModel>{
                        new EkycButtonModel{
                            type="primary",
                            itemNo=1,
                            text = "Onayla",
                            action="exit",
                            transition = "amorphie-ekyc-exit"
                        },
                        new EkycButtonModel{
                            type="secondary",
                            itemNo=2,
                            text="Görüntülü Görüşmeye Devam Et",
                            action="cancel"
                        }
                    }
                },
                buttons = new List<EkycButtonModel>()  
            }
        };



        targetObject.TriggeredBy = Guid.Parse(body.GetProperty($"TRX-{transitionName}").GetProperty("TriggeredBy").ToString());
        targetObject.TriggeredByBehalfOf = Guid.Parse(body.GetProperty($"TRX-{transitionName}").GetProperty("TriggeredByBehalfOf").ToString());
        variables.Add($"TRX{transitionName.ToString().Replace("-", "")}", targetObject);
        return Results.Ok(variables);

    }


}
