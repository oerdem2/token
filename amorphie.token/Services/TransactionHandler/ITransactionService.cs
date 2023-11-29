using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using amorphie.token.core.Models.Consent;

namespace amorphie.token.Services.TransactionHandler
{
    public interface ITransactionService
    {
        public core.Models.Transaction.Transaction? Transaction{get;}
        public Task<ServiceResponse> CheckLogin(string username,string password);
        public Task<ServiceResponse> CheckLoginFromWorkflow(string username,string password);
        public Task<ServiceResponse> GetTransaction(Guid id);
        public Task<ServiceResponse> ReloadTransaction();
        public Task<ServiceResponse> SaveTransaction(core.Models.Transaction.Transaction transaction);
        public ServiceResponse<LoginResponse> GetUser();
        public ServiceResponse<ConsentResponse> GetConsent();
        public Task<ServiceResponse> SaveUser(LoginResponse user);
        public Task<ServiceResponse> SaveConsent(ConsentResponse consent);
    }
}