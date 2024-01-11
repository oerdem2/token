using System.Dynamic;
using System.Security.Cryptography;
using System.Text;
using amorphie.token.Services.TransactionHandler;
using Newtonsoft.Json;

namespace amorphie.token.Middlewares
{
    public class TransactionMiddleware
    {
        private readonly RequestDelegate _next;
        public TransactionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, DaprClient daprClient, IConfiguration configuration)
        {

            var jobKey = context.Request.Headers.FirstOrDefault(h => h.Key.Equals("X-Zeebe-Job-Key")).Value;
            dynamic variables = new ExpandoObject();
            try
            {
                await _next.Invoke(context);
            }
            catch (ZeebeWorkerException ex)
            {
                variables.jobKey = long.Parse(jobKey);
                variables.errorCode = "exception-error";
                variables.errorMessage = ex.Message;

                try
                {
                    await daprClient.InvokeBindingAsync(configuration["ZeebeCommand"], "throw-error", variables);
                }
                catch (System.Exception ex2)
                {
                    throw ex;
                }
                context.Response.StatusCode = 200;
                context.Response.ContentType = "application/json";
            }
            catch (Exception ex)
            {
                variables.jobKey = long.Parse(jobKey);
                variables.errorCode = "exception-error";
                variables.errorMessage = ex.Message;

                await daprClient.InvokeBindingAsync(configuration["ZeebeCommand"], "throw-error", variables);
                context.Response.StatusCode = 200;
                context.Response.ContentType = "application/json";
            }
        }


    }

    public static class TransactionMiddlewareExtensions
    {
        public static IApplicationBuilder UseTransactionMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseWhen(c => c.Request.Path.ToString().StartsWith("/amorphie-login"), builder => builder.UseMiddleware<TransactionMiddleware>());
        }
    }
}