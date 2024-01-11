using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace amorphie.token.core.Models.Client;

public class ValidateClientByCodeRequest
{
    public string Code { get; set; }
    public string Secret { get; set; }
}
