using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Amazon.Runtime;
using amorphie.token.core.Extensions;
using amorphie.token.core.Models.LegacySSO;
using Newtonsoft.Json;
using VaultSharp.V1.SecretsEngines.Identity;

namespace amorphie.token.Services.LegacySSO
{
    public class LegacySSOService : ServiceBase, ILegacySSOService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public LegacySSOService(ILogger<LegacySSOService> logger, IConfiguration configuration, IHttpClientFactory httpClientFactory) : base(logger, configuration)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<ServiceResponse<List<SsoCredential>>> GetSsoCredentials(string username, string appCode, string type)
        {
            var ssoClient = _httpClientFactory.CreateClient("SSO");
            var content = new StringContent(CreateRequestBody(username,appCode,type),Encoding.UTF8,"application/soap+xml");
            try
            {
                var httpResponse = await ssoClient.PostAsync("?op=GetAuthorityForUser",content);
                if(httpResponse.IsSuccessStatusCode)
                {
                    var responseBody = await httpResponse.Content.ReadAsStringAsync();
                    var credentials = responseBody.GetWithRegexMultiple("(<Table1[^>]*>)(.*?)(</Table1>)",2);
                    var responseList = new List<SsoCredential>();
                    foreach(var credential in credentials)
                    {
                        XmlDocument xmlDocument = new XmlDocument();
                        xmlDocument.LoadXml("<root>" + credential + "</root>");
                        var serializedJson = JsonConvert.SerializeXmlNode(xmlDocument);
                        var credentialInfo = JsonConvert.DeserializeObject<SsoCredentialRoot>(serializedJson);
                        if(credentialInfo.root.PropertyId > 0 && !string.IsNullOrWhiteSpace(credentialInfo.root.Name) && !string.IsNullOrWhiteSpace(credentialInfo.root.Value))
                        {
                            responseList.Add(credentialInfo.root);
                        }
                    }
                    return new ServiceResponse<List<SsoCredential>>
                    {
                        Response = responseList,
                        StatusCode = 200,
                        Detail = ""
                    };
                }
                else
                {
                    return new ServiceResponse<List<SsoCredential>>
                    {
                        StatusCode = (int)httpResponse.StatusCode,
                        Detail = await httpResponse.Content.ReadAsStringAsync()
                    };
                }    
            }
            catch (Exception ex)
            {
                return new ServiceResponse<List<SsoCredential>>
                    {
                        StatusCode = 500,
                        Detail = ex.ToString()
                    };
            }
        }

        private string CreateRequestBody(string username, string appCode, string type)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            stringBuilder.Append("<soap12:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"  xmlns:soap12=\"http://www.w3.org/2003/05/soap-envelope\">");
            stringBuilder.Append("<soap12:Body>");
            stringBuilder.Append("<GetAuthorityForUser xmlns=\"http://tempuri.org/\">");
            stringBuilder.Append($"<applicationCode>{appCode}</applicationCode>");
            stringBuilder.Append($"<authorityName>{type}</authorityName>");
            stringBuilder.Append($"<loginAndDomainName>{username}</loginAndDomainName>");
            stringBuilder.Append("</GetAuthorityForUser>");
            stringBuilder.Append("</soap12:Body>");
            stringBuilder.Append("</soap12:Envelope>");
            return stringBuilder.ToString();
        }

        public  T DeserilazeFromXml<T>(string xml)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                
                    return (T)serializer.Deserialize(new StringReader(xml));
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occur during XML Deserialization :{ex.Message}");
            }
            return default(T);
        }
    }
}