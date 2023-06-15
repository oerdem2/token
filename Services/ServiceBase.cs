using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthServer.Services;

public class ServiceBase
{
    protected readonly IConfiguration Configuration;
    protected readonly ILogger Logger;
    public ServiceBase(ILogger logger,IConfiguration configuration)
    {
        Configuration = configuration;
        Logger = logger;
    }
}
