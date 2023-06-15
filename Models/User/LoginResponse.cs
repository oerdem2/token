using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AuthServer.Models.User;

public class LoginResponse
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    // public string Password { get; set; } = string.Empty;
    public string EMail { get; set; } = string.Empty;
    public Phone? MobilePhone { get; set; } = new Phone();

    public string Reference { get; set; } = string.Empty;
}

public class Phone
{
    public string CountryCode{get;set;}
    public string Prefix{get;set;}
    public string Number{get;set;}

    public override string ToString()
    {
        return $"{CountryCode}{Prefix}{Number}";
    }
}
