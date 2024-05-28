using amorphie.token.core.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Xml;
using System.Net;
using System.ServiceModel.Security;
using System.Security.Cryptography.X509Certificates;
namespace amorphie.token.core;

public class ServiceCaller
{
    private readonly ILogger<ServiceCaller> _logger;

    public ServiceCaller(ILogger<ServiceCaller> logger)
    {
        _logger = logger;
    }


    private BasicHttpBinding GetBasicBinding(string url, string useProxyServer = null, bool useBasicCredential = false, TimeSpan? openTimeout = null, TimeSpan? closeTimeout = null, TimeSpan? sendTimeout = null)
    {
        BasicHttpSecurityMode securityMode = BasicHttpSecurityMode.None;
        if (url.StartsWith("https://"))
        {
            securityMode = BasicHttpSecurityMode.Transport;
        }

        BasicHttpBinding basicHttpBinding = new BasicHttpBinding(securityMode)
        {
            MaxReceivedMessageSize = 2147483647L,
            MaxBufferSize = int.MaxValue,
            OpenTimeout = (openTimeout ?? TimeSpan.FromSeconds(240)),
            CloseTimeout = (closeTimeout ?? TimeSpan.FromSeconds(240)),
            ReceiveTimeout = TimeSpan.FromMinutes(20.0),
            SendTimeout = (sendTimeout ?? TimeSpan.FromSeconds(240)),
            ReaderQuotas = XmlDictionaryReaderQuotas.Max
        };
        if (!string.IsNullOrEmpty(useProxyServer))
        {
            basicHttpBinding.ProxyAddress = new Uri(useProxyServer);
            basicHttpBinding.UseDefaultWebProxy = false;
        }

        if (useBasicCredential)
        {
            basicHttpBinding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;
            basicHttpBinding.Security.Message.ClientCredentialType = BasicHttpMessageCredentialType.UserName;
        }

        return basicHttpBinding;
    }

    public virtual void Call<TServiceClient>(string url, Action<TServiceClient> action, bool useCustomBinding = false, object requestForLogging = null, TimeSpan? openTimeout = null, TimeSpan? closeTimeout = null, TimeSpan? sendTimeout = null)
    {
        Binding binding = null;
        binding = ((!useCustomBinding) ? ((Binding)GetBasicBinding(url, null, useBasicCredential: false, openTimeout, closeTimeout, sendTimeout)) : ((Binding)getCustomBinding(openTimeout, closeTimeout, sendTimeout)));
        using ChannelFactory<TServiceClient> channelFactory = new ChannelFactory<TServiceClient>(binding, new EndpointAddress(url));
        Stopwatch stopwatch = new Stopwatch();
        TServiceClient val = channelFactory.CreateChannel();
        try
        {
            stopwatch.Start();
            action(val);
            stopwatch.Stop();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError($"{url}, {ex.Message}, {action.Method}, {stopwatch.ElapsedMilliseconds}, {requestForLogging}");
            throw CatchException(url, ex, action.Method, stopwatch.ElapsedMilliseconds, requestForLogging);
        }
        finally
        {
            _logger.LogInformation(32032, "{Url} call lasted {ElapsedMilliseconds} milliseconds for {Action}.", url, stopwatch.ElapsedMilliseconds, cleanupMethodName(action.Method.Name));
            disposeChannel(val);
        }
    }

    public virtual async Task CallAsync<TServiceClient>(string url, Func<TServiceClient, Task> action, bool useCustomBinding = false, object requestForLogging = null, TimeSpan? openTimeout = null, TimeSpan? closeTimeout = null, TimeSpan? sendTimeout = null)
    {
        Binding binding = ((!useCustomBinding) ? ((Binding)GetBasicBinding(url, null, useBasicCredential: false, openTimeout, closeTimeout, sendTimeout)) : ((Binding)getCustomBinding(openTimeout, closeTimeout, sendTimeout)));
        using ChannelFactory<TServiceClient> factory = new ChannelFactory<TServiceClient>(binding, new EndpointAddress(url));
        Stopwatch stopwatch = new Stopwatch();
        TServiceClient proxy = factory.CreateChannel();
        try
        {
            stopwatch.Start();
            await action(proxy);
            stopwatch.Stop();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            throw CatchException(url, ex, action.Method, stopwatch.ElapsedMilliseconds, requestForLogging);
        }
        finally
        {
            _logger.LogInformation(32032, "{Url} call lasted {ElapsedMilliseconds} milliseconds for {Action}.", url, stopwatch.ElapsedMilliseconds, cleanupMethodName(action.Method.Name));
            disposeChannel(proxy);
        }
    }

    public virtual TServiceResponse Call<TServiceClient, TServiceResponse>(string url, Func<TServiceClient, TServiceResponse> action, bool useCustomBinding = false, object requestForLogging = null, string useProxyServer = "", TimeSpan? openTimeout = null, TimeSpan? closeTimeout = null, TimeSpan? sendTimeout = null)
    {
        Binding binding = null;
        binding = ((!useCustomBinding) ? ((Binding)GetBasicBinding(url, useProxyServer, useBasicCredential: false, openTimeout, closeTimeout, sendTimeout)) : ((Binding)getCustomBinding(openTimeout, closeTimeout, sendTimeout)));
        using ChannelFactory<TServiceClient> channelFactory = new ChannelFactory<TServiceClient>(binding, new EndpointAddress(url));
        Stopwatch stopwatch = new Stopwatch();
        TServiceClient val = channelFactory.CreateChannel();
        try
        {
            stopwatch.Start();
            TServiceResponse result = action(val);
            stopwatch.Stop();
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            throw CatchException(url, ex, action.Method, stopwatch.ElapsedMilliseconds, requestForLogging);
        }
        finally
        {
            _logger.LogInformation(32032, "{Url} call lasted {ElapsedMilliseconds} milliseconds for {Action}.", url, stopwatch.ElapsedMilliseconds, cleanupMethodName(action.Method.Name));
            disposeChannel(val);
        }
    }

    public virtual async Task<TServiceResponse> CallAsync<TServiceClient, TServiceResponse>(string url, Func<TServiceClient, Task<TServiceResponse>> action, bool useCustomBinding = false, object requestForLogging = null, string useProxyServer = "", TimeSpan? openTimeout = null, TimeSpan? closeTimeout = null, TimeSpan? sendTimeout = null)
    {

        
        Binding binding = null;
        binding = ((!useCustomBinding) ? ((Binding)GetBasicBinding(url, useProxyServer, useBasicCredential: false, openTimeout, closeTimeout, sendTimeout)) : ((Binding)getCustomBinding(openTimeout, closeTimeout, sendTimeout)));

        var endpointAddress = new EndpointAddress(url);
        var channelFactory = new ChannelFactory<TServiceClient>(binding, endpointAddress);
        // Sertifika doğrulamasını devre dışı bırak Bu kısım prod da kaldırılacak!!!
        channelFactory.Credentials.ServiceCertificate.SslCertificateAuthentication =
            new X509ServiceCertificateAuthentication
            {
                CertificateValidationMode = X509CertificateValidationMode.None,
                RevocationMode = X509RevocationMode.NoCheck
            };
        // using ChannelFactory<TServiceClient> channelFactory = new ChannelFactory<TServiceClient>(binding, new EndpointAddress(url));
        
        using (channelFactory){
               Stopwatch stopwatch = new Stopwatch();
        TServiceClient val = channelFactory.CreateChannel();
        try
        {
            stopwatch.Start();
            TServiceResponse result = await action(val);
            stopwatch.Stop();
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            throw CatchException(url, ex, action.Method, stopwatch.ElapsedMilliseconds, requestForLogging);
        }
        finally
        {
            _logger.LogInformation(32032, "{Url} call lasted {ElapsedMilliseconds} milliseconds for {Action}.", url, stopwatch.ElapsedMilliseconds, cleanupMethodName(action.Method.Name));
            disposeChannel(val);
        }
        }
     
    }

    private ServiceCallException CatchException(string url, Exception ex, MemberInfo methodInfo, long elapsedMilliseconds, object requestForLogging = null)
    {
        string logParams = null;
        string methodName = cleanupMethodName(methodInfo.Name);
        if (requestForLogging != null)
        {
            logParams = JsonConvert.SerializeObject(requestForLogging, new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver(),
                NullValueHandling = NullValueHandling.Include,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });
        }

        if (ex is FaultException)
        {
            XmlElement detail = ((FaultException)ex).CreateMessageFault().GetDetail<XmlElement>();
            ex.Data.Add("FaultExceptionData", detail.OuterXml);
        }



        return new ServiceCallException("Service call (" + url + ") with params failed with exception", url, methodName, logParams, elapsedMilliseconds, ex);
    }

    private void disposeChannel<TServiceClient>(TServiceClient proxy)
    {
        IClientChannel clientChannel = proxy as IClientChannel;
        try
        {
            clientChannel?.Dispose();
        }
        catch (Exception)
        {
            _logger.LogError(32032, "Could not dispose proxy object.");
        }
    }

    private CustomBinding getCustomBinding(TimeSpan? openTimeout = null, TimeSpan? closeTimeout = null, TimeSpan? sendTimeout = null)
    {
        HttpsTransportBindingElement httpsTransportBindingElement = new HttpsTransportBindingElement
        {
            MaxReceivedMessageSize = 65536000L,
            MaxBufferSize = 65536000
        };
        TextMessageEncodingBindingElement textMessageEncodingBindingElement = new TextMessageEncodingBindingElement
        {
            MessageVersion = MessageVersion.Soap12WSAddressing10
        };
        CustomBinding customBinding = new CustomBinding(textMessageEncodingBindingElement, httpsTransportBindingElement);
        customBinding.OpenTimeout = openTimeout ?? TimeSpan.FromSeconds(240);
        customBinding.CloseTimeout = closeTimeout ?? TimeSpan.FromSeconds(240);
        customBinding.ReceiveTimeout = TimeSpan.FromMinutes(20.0);
        customBinding.SendTimeout = sendTimeout ?? TimeSpan.FromSeconds(240);
        customBinding.CreateBindingElements();
        return customBinding;
    }

    private string cleanupMethodName(string actionMethodName)
    {
        int num = actionMethodName.IndexOf('<');
        int num2 = actionMethodName.IndexOf('>');
        if (num >= 0 && num2 >= 0 && num < num2)
        {
            return actionMethodName.Substring(num + 1, num2 - num - 1);
        }

        return actionMethodName;
    }
}
