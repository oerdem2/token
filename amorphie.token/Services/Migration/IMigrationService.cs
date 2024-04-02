using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace amorphie.token.Services.Migration
{
    public interface IMigrationService
    {
        public Task<ServiceResponse> MigrateUserData(Guid userId, Guid dodgeUserId);
        public Task<ServiceResponse> MigrateStaticData();
    }
}