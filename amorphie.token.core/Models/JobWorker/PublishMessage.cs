using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace amorphie.token.core.Models.JobWorker
{
    public class PublishMessage
    {
        public Guid TransactionId{get;set;}
        public string? Messages{get;set;}
        public string? ValueToCheck{get;set;}
        public string? CheckType{get;set;}
    }
}