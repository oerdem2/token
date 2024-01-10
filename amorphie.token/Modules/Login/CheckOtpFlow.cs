
using System.Dynamic;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace amorphie.token.Modules;

public static class CheckOtpFlow
{
    public static async Task<IResult> checkOtpFlow(
    [FromBody] dynamic body,
    [FromServices] IAuthorizationService authorizationService,
    [FromServices] IUserService userService,
    IConfiguration configuration,
    DaprClient daprClient
    )
    {
        var transactionId = body.GetProperty("InstanceId").ToString();
        var entityData = body.GetProperty("TRXamorphiemobileloginsendotp").GetProperty("Data").GetProperty("entityData").ToString();

        var entityObj = JsonSerializer.Deserialize<Dictionary<string, object>>(entityData);
        var providedCode = entityObj["otpValue"].ToString();
        var generatedCode = await daprClient.GetStateAsync<string?>(configuration["DAPR_STATE_STORE_NAME"], $"{transactionId}_Login_Otp_Code");

        var clientInfoSerialized = body.GetProperty("clientSerialized").ToString();

        ClientResponse clientInfo = JsonSerializer.Deserialize<ClientResponse>(clientInfoSerialized, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var userInfoSerialized = body.GetProperty("userSerialized").ToString();

        LoginResponse userInfo = JsonSerializer.Deserialize<LoginResponse>(userInfoSerialized, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (generatedCode != null && providedCode == generatedCode)
        {
            dynamic variables = new ExpandoObject();
            variables.otpMatch = true;
            
            var deviceId = body.GetProperty("Headers").GetProperty("xdeviceid").ToString();
            var installationId = body.GetProperty("Headers").GetProperty("xtokenid").ToString();
            var platform = body.GetProperty("Headers").GetProperty("xdeployment").ToString();
            var model = body.GetProperty("Headers").GetProperty("xdeviceinfo").ToString();
            await userService.SaveDevice(new UserSaveMobileDeviceDto()
            {
                DeviceId = deviceId,
                InstallationId = Guid.Parse(installationId),
                DeviceModel = model,
                DevicePlatform = platform,
                ClientId = clientInfo.code ?? clientInfo.id,
                UserId = userInfo.Id
            });
            return Results.Ok(variables);
        }
        else
        {
            var otpTryCount = Convert.ToInt32(body.GetProperty("OtpTryCount").ToString());
            dynamic variables = new ExpandoObject();
            variables.otpMatch = false;
            variables.OtpTryCount = otpTryCount++;
            variables.message = "Otp Check Failed";
            Console.WriteLine("CheckOtp Error " + JsonSerializer.Serialize(variables));
            return Results.Ok(variables);
        }
    }


}
