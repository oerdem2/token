using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using amorphie.token.core.Models.InternetBanking;
using amorphie.token.data;

namespace amorphie.token.Services.Migration
{
    public class MigrationService:ServiceBase,IMigrationService
    {
        private readonly IbDatabaseContext _ibDatabaseContext;
        public MigrationService(IbDatabaseContext ibDatabaseContext, IConfiguration configuration, ILogger<MigrationService> logger) : base(logger, configuration)
        {
            _ibDatabaseContext = ibDatabaseContext;
        }

        public Task<ServiceResponse> MigrateUserData(string username)
        {
            throw new Exception();
        }

    }
}