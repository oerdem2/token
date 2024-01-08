
using System.Dynamic;
using System.Text.Json;
using amorphie.token.core.Models.InternetBanking;
using amorphie.token.data;
using amorphie.token.Services.InternetBanking;
using Microsoft.AspNetCore.Mvc;

namespace amorphie.token.Modules;

public static class DisableUser
{
    public static async Task<IResult> disableUser(
    [FromBody] dynamic body,
    IbDatabaseContext ibContext,
    IInternetBankingUserService internetBankingUserService
    )
    {
        var transactionId = body.GetProperty("InstanceId").ToString();

        var requestBodySerialized = body.GetProperty($"TRXamorphiemobilelogin").GetProperty("Data").GetProperty("entityData").ToString();
        TokenRequest request = JsonSerializer.Deserialize<TokenRequest>(requestBodySerialized);


        var userResponse = await internetBankingUserService.GetUser(request.Username!);
        var user = userResponse.Response;

        IBStatus status = new();
        status.UserId = user.Id;
        status.CreatedByInstanceId = Guid.Parse(transactionId);
        status.CreatedByUserName = "Amorphie";
        status.State = 10;
        status.Reason = 6;
        status.Type = 30;
        status.ReasonDescription = "Locked After Several Attemps | Amorphie";
        await ibContext.Status.AddAsync(status);
        await ibContext.SaveChangesAsync();

        dynamic variables = new ExpandoObject();
        variables.status = true;
        
        return Results.Ok(variables);
    }


}
