using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace amorphie.token.Services.FlowHandler
{
    public interface IFlowHandler
    {
        public Task<ServiceResponse> StartOtpFlow(core.Models.Transaction.Transaction transaction);
        public Task<ServiceResponse> CheckOtp(string otpValue);
    }
}