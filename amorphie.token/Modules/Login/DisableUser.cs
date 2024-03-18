
using System.Dynamic;
using System.Text.Json;
using amorphie.core.Zeebe.dapr;
using amorphie.token.core.Models.InternetBanking;
using amorphie.token.data;
using amorphie.token.Services.InternetBanking;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace amorphie.token.Modules;

public static class DisableUser
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public static async Task<IResult> disableUser(
    [FromBody] dynamic body,
    IbDatabaseContext ibContext,
    IInternetBankingUserService internetBankingUserService
    )
    {
        var langCode = ErrorHelper.GetLangCode(body);
        var transactionId = body.GetProperty("InstanceId").ToString();

        var requestBodySerialized = body.GetProperty("requestBody").ToString();
        TokenRequest request = JsonSerializer.Deserialize<TokenRequest>(requestBodySerialized);


        var userResponse = await internetBankingUserService.GetUser(request.Username!);
        var user = userResponse.Response;

        var lastStatus = await ibContext.Status.Where(s => s.UserId == user!.Id && (!s.State.HasValue || s.State.Value == 10)).OrderByDescending(s => s.CreatedAt).FirstOrDefaultAsync();

        dynamic variables = new ExpandoObject();
        if (lastStatus?.Type == 30 || lastStatus?.Type == 40)
        {
            variables.status = true;
            return Results.Ok(variables);
        }
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


        variables.status = true;
        variables.message = ErrorHelper.GetErrorMessage(LoginErrors.BlockedUser,langCode);
        return Results.Ok(variables);
    }


}
