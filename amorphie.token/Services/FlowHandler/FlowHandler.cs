using amorphie.token.core.Enums.MessagingGateway;
using amorphie.token.core.Models.MessagingGateway;
using amorphie.token.Services.MessagingGateway;
using amorphie.token.Services.TransactionHandler;
using Refit;

namespace amorphie.token.Services.FlowHandler
{
    public class FlowHandler : ServiceBase, IFlowHandler
    {
        private readonly ITransactionService _transactionService;
        private readonly IMessagingGateway _messagingGateway;
        private readonly DaprClient _daprClient;

        public FlowHandler(ITransactionService transactionService, IMessagingGateway messagingGateway, DaprClient daprClient, IConfiguration configuration, ILogger<FlowHandler> logger) : base(logger, configuration)
        {
            _transactionService = transactionService;
            _daprClient = daprClient;
            _messagingGateway = messagingGateway;
        }

        public async Task<ServiceResponse> CheckOtp(string otpValue)
        {
            
            return null;
        }

        public async Task<ServiceResponse> StartOtpFlow(core.Models.Transaction.Transaction transaction)
        {
            return null;
        }



    }
}