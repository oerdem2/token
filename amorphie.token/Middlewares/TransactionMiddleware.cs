using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Transactions;
using amorphie.token.Services.InternetBanking;
using amorphie.token.Services.Profile;
using amorphie.token.Services.Transaction;
using Google.Api;
using Microsoft.Extensions.Primitives;
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

        public async Task InvokeAsync(HttpContext context,ITransactionService transactionService)
        {
            var requestBody = await GetRequestBody(context);
            var actualUsername = "";
            var actualPassword = "";
            
            if(context.Request.Headers.ContainsKey("InstanceId"))
            {
                var transactionId = Guid.Parse(context.Request.Headers.FirstOrDefault(h => h.Equals("InstanceId")).Value);
                await transactionService.GetTransaction(transactionId);
            }

            if(context.Request.Method == "GET")
            {
                if(!context.Request.Query.ContainsKey("transactionId"))
                {
                    var transaction = new core.Models.Transaction.Transaction()
                    {
                        Id = Guid.NewGuid(),
                        Request = requestBody,
                        TransactionState = TransactionState.Active 
                    };

                    if(context.Request.Path.ToString().Contains("OpenBanking"))
                    {
                        transaction.TransactionType = TransactionType.OpenBanking;
                    }

                    var response = await transactionService.SaveTransaction(transaction);
                }
                else
                {
                    var transactionId = context.Request.Query["transactionId"];
                    var response = await transactionService.GetTransaction(Guid.Parse(transactionId));
                }
            }

            if(context.Request.HasFormContentType)
            {
                IFormCollection form = await context.Request.ReadFormAsync();
                
                if(form.ContainsKey("transactionId"))
                {
                    var response = await transactionService.GetTransaction(Guid.Parse(form["transactionId"]));
                }

                if(form.ContainsKey("username") && form.ContainsKey("password"))
                {
                    actualUsername = form["username"];
                    actualPassword = form["password"];
                    
                    // if(!string.IsNullOrWhiteSpace(actualUsername) && !string.IsNullOrWhiteSpace(actualPassword))
                    // {
                    //     Dictionary<string,StringValues> formParams = new Dictionary<string, StringValues>();
                    //     foreach(var item in form)
                    //     {
                    //         if(item.Key != "username" && item.Key != "password")
                    //         {
                    //             formParams.Add(item.Key,item.Value);
                    //         }
                    //     }
                    //     formParams.Add("username","password");
                    //     formParams.Add("password","password");
                    //     context.Request.Form = new FormCollection(formParams);
                    // }
                }
            }
            if(context.Request.HasJsonContentType())
            {
                var middlewareRequest = JsonConvert.DeserializeObject<MiddlewareRequest>(requestBody);

                if(middlewareRequest.transactionId != null)
                {
                    var response = await transactionService.GetTransaction(middlewareRequest.transactionId);
                }

                if(middlewareRequest.username != null && middlewareRequest.password != null)
                {
                    
                    actualUsername = middlewareRequest.username;
                    actualPassword = middlewareRequest.password;
                    // if(!string.IsNullOrWhiteSpace(middlewareRequest.username) && !string.IsNullOrWhiteSpace(middlewareRequest.password))
                    // {
                    //     var dynamicJson = JsonConvert.DeserializeObject<dynamic>(requestBody);
                    //     dynamicJson.username = "2133";
                    //     dynamicJson.password = "pass";
                    //     var stream = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(dynamicJson)));
                    //     context.Request.Body = stream;
                    // }
                }
            }
            
            var migrateUser = await transactionService.CheckLogin(actualUsername,actualPassword);

            await _next.Invoke(context);
        }

        private async Task<string> GetRequestBody(HttpContext context)
        {
            var requestBody = "";
            context.Request.EnableBuffering();
            using (var reader = new StreamReader(context.Request.Body, System.Text.Encoding.UTF8, true, 1024, true))
            {
                requestBody = await reader.ReadToEndAsync();
            }
            context.Request.Body.Position = 0;

            return requestBody;
        }

     

        
    }

    public class MiddlewareRequest
    {
        public string username{get;set;}
        public string password{get;set;}
        public Guid transactionId{get;set;}
    }

    public static class TransactionMiddlewareExtensions
    {
        public static IApplicationBuilder UseTransactionMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TransactionMiddleware>();
        }
    }
}