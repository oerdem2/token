

using Microsoft.AspNetCore.Mvc;
using System.Dynamic;

namespace amorphie.token.Modules;

public static class DaprTest
{
    public static void MapDaprTestControlEndpoints(this WebApplication app)
    {
        app.MapPost("/start-workflow", startWorkflow)
        .Produces(StatusCodes.Status200OK);

        app.MapPost("/introspect",introspect)
        .Produces(StatusCodes.Status200OK);

        app.MapGet("/secured",secured)
        .Produces(StatusCodes.Status200OK);

        app.MapPost("/checkOtp",confirmOtp)
        .Produces(StatusCodes.Status200OK);

        static async Task<IResult> secured(
            HttpRequest request
        )
        {
            foreach (var header in request.Headers)
            {
                Console.WriteLine($"Introspect header {header.Key}:{header.Value} ");
            }
            return Results.Ok(new{token="valid"});
        }

 

        static async Task<IResult> introspect(
        [FromBody] dynamic data,HttpRequest request
        )
        {
            foreach (var header in request.Headers)
            {
                Console.WriteLine($"Introspect header {header.Key}:{header.Value} ");
            }
            return Results.Ok(new{active = true,scope = "test mest"});
        }

        static async Task<IResult> startWorkflow(
        [FromServices] DaprClient daprClient,
        [FromBody] dynamic body,
        [FromServices] IUserService  userService
        )
        {
            var userResponse = await userService.Login(new LoginRequest(){Reference = "123",Password = "21125"});

            dynamic messageData = new ExpandoObject();

            dynamic data = new Dictionary<string,dynamic>();
            dynamic targetObj = new ExpandoObject();

            data.Add("transactionId","12312512512616161");
            data.Add("LastTransition","send-otp-login-flow");
            data.Add("InstanceId","121321321");
            targetObj.Data = new ExpandoObject();
            targetObj.Data.entityData = body;
            data.Add("TRX-send-otp-login-flow",targetObj);
            messageData.messageName  = "start-password-flow";
            messageData.variables = data;
            await daprClient.InvokeBindingAsync<dynamic,dynamic>("zeebe-local","publish-message",messageData);
            return Results.Ok();
        }

        static async Task<IResult> confirmOtp(
        [FromServices] DaprClient daprClient,
        [FromBody] dynamic body
        )
        {
            dynamic messageData = new ExpandoObject();

            dynamic data = new ExpandoObject();
            
            data.otpValue = body.GetProperty("otpValue").ToString();;

            messageData.messageName  = "send-otp-login-flow";
            messageData.variables = data;
            messageData.correlationKey = "12312512512616161";
            
            await daprClient.InvokeBindingAsync<dynamic,dynamic>("zeebe-local","publish-message",messageData);
            return Results.Ok();
        }

    }
}
