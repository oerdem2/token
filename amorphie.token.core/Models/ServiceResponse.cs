using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace amorphie.token.core.Models;

public class ServiceResponse<T>
{
    public int StatusCode { get; set; }
    public string Detail { get; set; } = "";
    public T? Response { get; set; }
}

public class ServiceResponse
{
    public int StatusCode { get; set; }
    public string Detail { get; set; } = "";
}
