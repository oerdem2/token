using System.Dynamic;
using amorphie.token.data;
using amorphie.token.Services.TransactionHandler;
using Elastic.Apm.Api;

namespace amorphie.token.Middlewares
{
    public class TransactionMiddleware
    {
        private readonly RequestDelegate _next;
        public TransactionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, DaprClient daprClient, IConfiguration configuration, ITransactionService transactionService)
        {

            var instanceKey = Convert.ToInt64(context.Request.Headers.FirstOrDefault(h => h.Key.Equals("X-Zeebe-Process-Instance-Key")).Value);
            var jobKey = Convert.ToInt64(context.Request.Headers.FirstOrDefault(h => h.Key.Equals("X-Zeebe-Job-Key")).Value);

            await transactionService.InitLogon(instanceKey, jobKey);

            dynamic variables = new ExpandoObject();
            try
            {
                await _next.Invoke(context);
            }
            catch (ZeebeWorkerException ex)
            {
                variables.jobKey = jobKey;
                variables.errorCode = "exception-error";
                variables.errorMessage = ex.Message;

                transactionService.Logon.Error = ex.Message;
                transactionService.Logon.LogonStatus = LogonStatus.Failed;
                try
                {
                    await daprClient.InvokeBindingAsync(configuration["ZeebeCommand"], "throw-error", variables);
                }
                catch 
                {
                    
                }
                context.Response.StatusCode = 200;
                context.Response.ContentType = "application/json";
            }
            catch (Exception ex)
            {
                variables.jobKey = jobKey;
                variables.errorCode = "exception-error";
                variables.errorMessage = ex.Message;

                transactionService.Logon.Error = ex.Message;
                transactionService.Logon.LogonStatus = LogonStatus.Failed;

                await daprClient.InvokeBindingAsync(configuration["ZeebeCommand"], "throw-error", variables);
                context.Response.StatusCode = 200;
                context.Response.ContentType = "application/json";
            }
            finally
            {
                try
                {
                    await transactionService.SaveLogon();
                }
                catch
                {

                }

            }
        }


    }

    public static class TransactionMiddlewareExtensions
    {
        public static IApplicationBuilder UseTransactionMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseWhen(c => c.Request.Method == "POST" && c.Request.Path.ToString().StartsWith("/amorphie-login"), builder => builder.UseMiddleware<TransactionMiddleware>());
        }
    }
}