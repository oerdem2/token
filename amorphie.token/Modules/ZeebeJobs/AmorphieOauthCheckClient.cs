
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using System.Dynamic;
using Microsoft.AspNetCore.Http.HttpResults;
using amorphie.token.Services.TransactionHandler;
using amorphie.token.core.Models.JobWorker;
using amorphie.token.Services.FlowHandler;

namespace amorphie.token.Modules.ZeebeJobs;

public static class ValidateClient
{
    public static void MapAmorphieOauthCheckClientEndpoint(this WebApplication app)
    {
        app.MapPost("/amorphie-oauth-check-client", checkClient)
        .Produces(StatusCodes.Status200OK);
        app.MapPost("/amorphie-oauth-check-response-type", checkResponseType)
        .Produces(StatusCodes.Status200OK);
        app.MapPost("/amorphie-oauth-check-scopes", checkScopes)
        .Produces(StatusCodes.Status200OK);
        app.MapPost("/amorphie-oauth-publish-message", publishMessage)
        .Produces(StatusCodes.Status200OK);
        app.MapPost("/amorphie-oauth-show-page", showPage)
        .Produces(StatusCodes.Status200OK);
        app.MapPost("/amorphie-oauth-check-user", checkUser)
        .Produces(StatusCodes.Status200OK);
        app.MapPost("/amorphie-oauth-generate-otp", generateOtp)
        .Produces(StatusCodes.Status200OK);

        static async Task<IResult> checkClient(
        [FromBody] dynamic body,
        [FromServices] IClientService clientService,
        [FromServices] DaprClient daprClient,
        HttpRequest request
        )
        {
            return Results.Ok(new { test = "123" });
            Guid.TryParse(body.GetProperty("TransactionId").ToString(), out Guid TransactionId);
            dynamic resultData = new ExpandoObject();
            if (Guid.TryParse(body.GetProperty("ClientId").ToString(), out Guid ClientId))
            {
                dynamic errorData = new ExpandoObject();
                errorData.jobKey = Convert.ToInt64(request.Headers["X-Zeebe-Job-Key"]);
                errorData.errorCode = "Error500";
                errorData.errorMessage = "ClientId Not Valid";
                await daprClient.InvokeBindingAsync("zeebe-local", "throw-error", errorData);
            }

            await Task.CompletedTask;
            return Results.Ok();
        }

        static async Task<IResult> checkResponseType(
        [FromBody] dynamic body,
        [FromServices] IClientService clientService,
        [FromServices] DaprClient daprClient,
        HttpRequest request
        )
        {
            return Results.Ok(new { test = "123" });
            Guid.TryParse(body.GetProperty("TransactionId").ToString(), out Guid TransactionId);
            dynamic resultData = new ExpandoObject();
            if (Guid.TryParse(body.GetProperty("ClientId").ToString(), out Guid ClientId))
            {
                dynamic errorData = new ExpandoObject();
                errorData.jobKey = Convert.ToInt64(request.Headers["X-Zeebe-Job-Key"]);
                errorData.errorCode = "Error500";
                errorData.errorMessage = "ClientId Not Valid";
                await daprClient.InvokeBindingAsync("zeebe-local", "throw-error", errorData);
            }

            await Task.CompletedTask;
            return Results.Ok();
        }

        static async Task<IResult> checkScopes(
        [FromBody] dynamic body,
        [FromServices] IClientService clientService,
        [FromServices] DaprClient daprClient,
        HttpRequest request
        )
        {
            return Results.Ok(new { test = "123" });
            Guid.TryParse(body.GetProperty("TransactionId").ToString(), out Guid TransactionId);
            dynamic resultData = new ExpandoObject();
            if (Guid.TryParse(body.GetProperty("ClientId").ToString(), out Guid ClientId))
            {
                dynamic errorData = new ExpandoObject();
                errorData.jobKey = Convert.ToInt64(request.Headers["X-Zeebe-Job-Key"]);
                errorData.errorCode = "Error500";
                errorData.errorMessage = "ClientId Not Valid";
                await daprClient.InvokeBindingAsync("zeebe-local", "throw-error", errorData);
            }

            await Task.CompletedTask;
            return Results.Ok();
        }

        static async Task<IResult> publishMessage(
        [FromBody] PublishMessage publishMessage,
        [FromServices] IClientService clientService,
        [FromServices] DaprClient daprClient,
        [FromServices] ITransactionService transactionService,
        HttpRequest request
        )
        {
            await transactionService.GetTransaction(publishMessage.TransactionId);
            var transaction = transactionService.Transaction;
            var messages = publishMessage.Messages!.Split("||");
            var valueToCheck = publishMessage.ValueToCheck;
            var checkType = publishMessage.CheckType;

            Type t = transaction!.GetType();

            var property = t.GetProperties().FirstOrDefault(p => p.Name.ToLower() == valueToCheck!.ToLower());

            if (checkType!.ToLower() == "isnull")
            {
                if (property!.GetValue(transaction) == null)
                {
                    transaction.TransactionNextMessage = messages[0];
                }
                else
                {
                    transaction.TransactionNextMessage = messages[1];
                }
            }
            Console.WriteLine("InWorker TransactionId: " + transaction.Id);
            transaction.TransactionNextEvent = TransactionNextEvent.PublishMessage;
            await transactionService.SaveTransaction(transaction);
            return Results.Ok(new { test = "123" });
        }

        static async Task<IResult> showPage(
        [FromBody] dynamic body,
        [FromServices] IClientService clientService,
        [FromServices] DaprClient daprClient,
        [FromServices] ITransactionService transactionService,
        HttpRequest request
        )
        {
            Guid.TryParse(body.GetProperty("TransactionId").ToString(), out Guid TransactionId);
            var pageName = body.GetProperty("PageName").ToString();
            await transactionService.GetTransaction(TransactionId);
            var transaction = transactionService.Transaction;

            transaction!.TransactionNextEvent = TransactionNextEvent.ShowPage;
            if (pageName.Equals("login"))
                transaction.TransactionNextPage = TransactionNextPage.Login;
            if (pageName.Equals("otp"))
                transaction.TransactionNextPage = TransactionNextPage.Otp;

            await transactionService.SaveTransaction(transaction);
            return Results.Ok(new { test = "123" });
        }

        static async Task<IResult> checkUser(
        [FromBody] dynamic body,
        [FromServices] IClientService clientService,
        [FromServices] DaprClient daprClient,
        [FromServices] ITransactionService transactionService,
        [FromServices] IUserService userService,
        HttpRequest request
        )
        {
            try
            {
                Guid.TryParse(body.GetProperty("TransactionId").ToString(), out Guid TransactionId);
                var username = body.GetProperty("username").ToString();
                var password = body.GetProperty("password").ToString();
                await transactionService.GetTransaction(TransactionId);
                var transaction = transactionService.Transaction;

                var userResult = await userService.Login(new LoginRequest() { Reference = username, Password = password });
                if (userResult.StatusCode == 200)
                {
                    transaction!.TransactionNextEvent = TransactionNextEvent.PublishMessage;
                    transaction.TransactionNextMessage = "amorphie-oauth-otp-flow";
                    transaction.User = userResult.Response;
                    await transactionService.SaveTransaction(transaction);
                    return Results.Ok(new { test = "123" });
                }
                else
                {
                    return Results.Ok(new { test = "123" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ex t:" + ex.ToString());
            }

            return Results.Ok(new { test = "123" });
        }

        static async Task<IResult> generateOtp(
        [FromBody] dynamic body,
        [FromServices] IClientService clientService,
        [FromServices] DaprClient daprClient,
        [FromServices] ITransactionService transactionService,
        [FromServices] IUserService userService,
        [FromServices] IFlowHandler flowHandler,
        HttpRequest request
        )
        {
            try
            {
                Guid.TryParse(body.GetProperty("TransactionId").ToString(), out Guid TransactionId);
                await transactionService.GetTransaction(TransactionId);

                var otpResult = await flowHandler.StartOtpFlow(transactionService.Transaction!);

            }
            catch (Exception ex)
            {
                Console.WriteLine("ex t:" + ex.ToString());
            }

            return Results.Ok(new { test = "123" });
        }
    }
}
