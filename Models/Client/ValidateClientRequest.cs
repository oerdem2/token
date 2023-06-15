using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthServer.Models.Client;

public class ValidateClientRequest
{
    public string ClientId{get;set;}
    public string Secret{get;set;}
}
