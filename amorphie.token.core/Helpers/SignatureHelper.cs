using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Jose;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace amorphie.token.core.Helpers
{
    public class SignatureHelper
    {
         /// <summary>
    /// Set X-JWS-Signature header property
    /// </summary>
    /// <param name="httpContext"></param>
    /// <param name="configuration"></param>
    /// <param name="body">Message body</param>
    public static void SetXJwsSignatureHeader(HttpContext httpContext, IConfiguration configuration, object body)
    {
        var headerPropData = GetXJwsSignature(body, configuration);
        httpContext.Response.Headers.Add("X-JWS-Signature", headerPropData);
    }

    public static string GetXJwsSignature(object body, IConfiguration configuration)
    {
        //JWT header-payload-signature

        // Create JWS header
        var jwtHeader = new Dictionary<string, object>()
        {
            { "alg", "RS256" },
            { "typ", "JWT" }
        };

        // Create JWS payload
        //From Document:
        //Payload kısmında özel olarak oluşturulacak olan “body” claim alanına istek gövdesi (request body) verisinin SHA256 hash değeri karşılığı yazılmalıdır.
        var data = new Dictionary<string, object>()
        {
            {"iss", "https://apigw.bkm.com.tr"},
            {"exp",  ((DateTimeOffset)DateTime.UtcNow.AddMinutes(60)).ToUnixTimeSeconds()},
            {"iat", ((DateTimeOffset)DateTime.UtcNow.AddMinutes(-5)).ToUnixTimeSeconds()},
            {"body", GetChecksumSHA256(body)}
        };

        // Load private key from file
        var key = LoadPrivateKeyFromVault(configuration);
        return Jose.JWT.Encode(payload: data, key: key, JwsAlgorithm.RS256, extraHeaders: jwtHeader);
    }

    private static RSA LoadPrivateKeyFromPemFile(string pemFilePath)
    {
        string pemContents;
        using (StreamReader reader = new StreamReader(pemFilePath))
        {
            pemContents = reader.ReadToEnd();
        }
        var key = RSA.Create();
        key.ImportFromPem(pemContents);
        return key;
    }

    /// <summary>
    /// Loads hhsprivate key from vault
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns>RSA Key</returns>
    private static RSA LoadPrivateKeyFromVault(IConfiguration configuration)
    {
        string pemContents = configuration["HHS_PrivateKey"];
        var key = RSA.Create();
        key.ImportFromPem(pemContents);
        return key;
    }

    /// <summary>
    /// Generates sha256 hash of body
    /// </summary>
    /// <param name="body"></param>
    /// <returns></returns>
    private static string GetChecksumSHA256(object body)
    {
        // Initialize a SHA256 hash object.
        using (SHA256 sha256Hash = SHA256.Create())
        {
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(body)));
            return Convert.ToHexString(bytes);
        }
    }
    }
}