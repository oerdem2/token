
namespace amorphie.token.core.Exceptions
{
    public class ZeebeWorkerException : Exception
    {
        public int ErrorCode { get; set; }
        public string ErrorMessage { get; set; }

        public ZeebeWorkerException(int errorCode, string errorMessage) : base(errorMessage)
        {
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
        }
    }
}