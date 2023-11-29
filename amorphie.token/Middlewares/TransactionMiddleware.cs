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

        public async Task InvokeAsync(HttpContext context,ITransactionService transactionService)
        {
            try
            {
                
                var requestBody = await GetRequestBody(context);
                
                var actualUsername = "";
                var actualPassword = "";
               
                if(context.Request.Method == "GET")
                {
                    if(!context.Request.Query.ContainsKey("transactionId"))
                    {
                        var transaction = new core.Models.Transaction.Transaction()
                        {
                            Id = Guid.NewGuid(),
                            TransactionState = TransactionState.Active 
                        };

                        if(context.Request.Path.ToString().Contains("OpenBanking"))
                        {
                            transaction.TransactionType = TransactionType.OpenBanking;
                        }
                        Console.WriteLine("save edilecek 1");
                        var response = await transactionService.SaveTransaction(transaction);
                    }
                    else
                    {
                        var transactionId = context.Request.Query["transactionId"];
                        var response = await transactionService.GetTransaction(Guid.Parse(transactionId!));
                    }
                }

                if(context.Request.HasFormContentType)
                {
                    IFormCollection form = await context.Request.ReadFormAsync();
                    
                    if(form.ContainsKey("transactionId"))
                    {
                        var response = await transactionService.GetTransaction(Guid.Parse(form["transactionId"]!));
                    }
                    else
                    {
                        var transaction = new core.Models.Transaction.Transaction()
                        {
                            Id = Guid.NewGuid(),
                            TransactionState = TransactionState.Active 
                        };

                        if(context.Request.Path.ToString().Contains("OpenBanking"))
                        {
                            transaction.TransactionType = TransactionType.OpenBanking;
                        }
                        Console.WriteLine("save edilecek 2");
                        var response = await transactionService.SaveTransaction(transaction);
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

                    if(middlewareRequest!.TransactionId != null)
                    {
                        if(Guid.TryParse(middlewareRequest!.TransactionId.ToString(),out Guid parsedTransactionId))
                        {
                            var response = await transactionService.GetTransaction(middlewareRequest.TransactionId!.Value);
                        }
                    }
                    else
                    {
                        var transaction = new core.Models.Transaction.Transaction()
                        {
                            Id = Guid.NewGuid(),
                            TransactionState = TransactionState.Active 
                        };

                        if(context.Request.Path.ToString().Contains("OpenBanking"))
                        {
                            transaction.TransactionType = TransactionType.OpenBanking;
                        }
                        Console.WriteLine("save edilecek 3");
                        var response = await transactionService.SaveTransaction(transaction);
                    }

                    if(middlewareRequest.Username != null && middlewareRequest.Password != null)
                    {
                        
                        actualUsername = middlewareRequest.Username;
                        actualPassword = middlewareRequest.Password;
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
                        
                var migrateUser = await transactionService.CheckLogin(actualUsername!,actualPassword!);
               
                await _next.Invoke(context);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                await _next.Invoke(context);
            }
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
        public string? Username{get;set;}
        public string? Password{get;set;}
        public Guid? TransactionId{get;set;}
    }

    public static class TransactionMiddlewareExtensions
    {
        public static IApplicationBuilder UseTransactionMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TransactionMiddleware>();
        }
    }
}