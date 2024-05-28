using Microsoft.Extensions.Logging;
using System.Runtime.Serialization;

namespace amorphie.token.core.Exceptions
{
    [Serializable]
    public class ServiceCallException : ApplicationException, ISerializable
    {
        public const string SERVICE_URL_KEY = "ServiceUrl";

        public const string METHOD_NAME_KEY = "Action";

        public const string SERVICE_PARAMETERS_KEY = "ServiceParameters";

        public const string ELAPSED_MILLISECONDS_KEY = "ElapsedMilliseconds";

        public EventId EventId { get; }

        public int HttpResponseStatusCode { get; protected set; } = 400;

        private bool IsLogged { get; set; }

        public string MethodName { get; private set; }

        public string Url { get; private set; }

        public string LogParams { get; private set; }

        public long ElapsedMilliseconds { get; private set; }

        public ServiceCallException(string message, string url, string methodName, string logParams, long elapsedMilliseconds, Exception innerException)
           
        {
            MethodName = methodName;
            Url = url;
            LogParams = logParams;
            ElapsedMilliseconds = elapsedMilliseconds;
            HttpResponseStatusCode = 500;
        }

        public ServiceCallException(EventId eventId, string message, string url, string methodName, string logParams, long elapsedMilliseconds, Exception innerException)
        {
            MethodName = methodName;
            Url = url;
            LogParams = logParams;
            ElapsedMilliseconds = elapsedMilliseconds;
            HttpResponseStatusCode = 500;
        }

        public ServiceCallException(EventId eventId, string message, string url, string methodName, string logParams, long elapsedMilliseconds)
            
        {
            MethodName = methodName;
            Url = url;
            LogParams = logParams;
            ElapsedMilliseconds = elapsedMilliseconds;
            HttpResponseStatusCode = 500;
        }

        protected ServiceCallException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Url = info.GetString("ServiceUrl");
            MethodName = info.GetString("Action");
            LogParams = info.GetString("ServiceParameters");
            ElapsedMilliseconds = info.GetInt64("ElapsedMilliseconds");
        }

        protected  void AdditionalObjectData(SerializationInfo info)
        {
            AdditionalObjectData(info);
            info.AddValue("ServiceUrl", Url);
            info.AddValue("Action", MethodName);
            info.AddValue("ServiceParameters", LogParams);
            info.AddValue("ElapsedMilliseconds", ElapsedMilliseconds);
        }

      
    }
}
