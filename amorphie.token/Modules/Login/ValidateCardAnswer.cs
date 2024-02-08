
using System.Dynamic;
using System.Text.Json;
using amorphie.token.core.Models.InternetBanking;
using amorphie.token.data;
using amorphie.token.Services.Card;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace amorphie.token.Modules;

public static class ValidateCardAnswer
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public static async Task<IResult> validateCardAnswer(
    [FromBody] dynamic body,
    ICardHandler cardHandler
    )
    {
        dynamic variables = new ExpandoObject();

        var transitionName = body.GetProperty("LastTransition").ToString();

        var ibUserSerialized = body.GetProperty("ibUserSerialized").ToString();
        IBUser ibUser = JsonSerializer.Deserialize<IBUser>(ibUserSerialized);

        var cardNo = body.GetProperty("cardNo").ToString();
        var cardCvv = body.GetProperty("cardCvv").ToString();
        var cardPin = body.GetProperty("cardPin").ToString();
        
        ServiceResponse cardResponse = await cardHandler.ValidateCard(ibUser.UserName,cardNo,cardCvv,cardPin);
        if(cardResponse.StatusCode == 200)
        {
            variables.isValidated = true;
        }
        else
        {
            variables.isValidated = false;
        }
        return Results.Ok(variables);
    }


}
