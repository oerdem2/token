using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using amorphie.token.core.Enums.MessagingGateway;
using amorphie.token.core.Models.MessagingGateway;
using amorphie.token.Services.MessagingGateway;
using amorphie.token.Services.Transaction;
using Refit;

namespace amorphie.token.Services.FlowHandler
{
    public class FlowHandler : ServiceBase, IFlowHandler
    {
        private readonly ITransactionService _transactionService;
        private readonly IMessagingGateway _messagingGateway;
        private readonly DaprClient _daprClient;

        public FlowHandler(ITransactionService transactionService,IMessagingGateway messagingGateway,DaprClient daprClient,IConfiguration configuration,ILogger<FlowHandler> logger) : base(logger,configuration)
        {
            _transactionService = transactionService;
            _daprClient = daprClient;
            _messagingGateway = messagingGateway;
        }

        public async Task<ServiceResponse> CheckOtp(string otpValue)
        {
            var response = new ServiceResponse();
            try
            {
                var sendedOtpValue = await _daprClient.GetStateAsync<string>(Configuration["DAPR_STATE_STORE_NAME"], $"{_transactionService.Transaction.Id}_Login_Otp_Code");
                if(sendedOtpValue.Equals(otpValue))
                {
                    response.StatusCode = 200;
                }
                else
                {
                    response.StatusCode = 403;
                    response.Detail = "Provided Otp Value Doesn't Match With Sended Otp";
                }
            }
            catch (System.Exception ex)
            {
                response.StatusCode = 500;
                response.Detail = ex.ToString();
            }

            return response;
        }

        public async Task<ServiceResponse> StartOtpFlow(core.Models.Transaction.Transaction transaction)
        {
            var response = new ServiceResponse();

            var rand = new Random();
            var code = String.Empty;

            for (int i = 0; i < 6; i++)
            {
                code += rand.Next(10);
            }

            await _daprClient.SaveStateAsync(Configuration["DAPR_STATE_STORE_NAME"], $"{_transactionService.Transaction.Id}_Login_Otp_Code", code);

            
            var otpRequest = new SmsRequest()
            {
                Sender = SenderType.AutoDetect,
                SmsType = SmsTypes.Otp,
                Phone = new PhoneString()
                {
                    CountryCode = transaction.UserInfo.phone.countryCode.ToString(),
                    Prefix = transaction.UserInfo.phone.prefix.ToString(),
                    Number = transaction.UserInfo.phone.number
                },
                Content = $"{code} şifresi ile giriş yapabilirsiniz",
                Process = new Process()
                {
                    Name = "Token Login Flow",
                    Identity = "Otp Login"
                }
            };
           
            try
            {
                var smsReponse = await _messagingGateway.SendSms(otpRequest);
                if(smsReponse.Status == "Success")
                {
                    response.StatusCode = 200;
                    var transactionResponse = await _transactionService.SaveTransaction(transaction);
                }
            }
            catch (ApiException ex)
            {
                response.StatusCode = (int)ex.StatusCode;
                response.Detail = ex.ToString();
            }
            catch (System.Exception ex)
            {
                response.StatusCode = 500;
                response.Detail = ex.ToString();
            }

            return response;
        }

        

    }
}