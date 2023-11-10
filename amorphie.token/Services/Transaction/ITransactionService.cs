using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace amorphie.token.Services.Transaction
{
    public interface ITransactionService
    {
        public core.Models.Transaction.Transaction Transaction{get;}
        public Task<ServiceResponse> CheckLogin(string username,string password);
        public Task<ServiceResponse> CheckLoginFromWorkflow(string username,string password);
        public Task<ServiceResponse> GetTransaction(Guid id);
        public Task<ServiceResponse> SaveTransaction(core.Models.Transaction.Transaction transaction);
    }
}