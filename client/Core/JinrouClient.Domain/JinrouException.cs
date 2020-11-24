using System;
namespace JinrouClient.Domain
{
    public class JinrouException : Exception
    {
        public ErrorCode ErrorCode { get; }

        public JinrouException(string message, ErrorCode errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }

        public JinrouException(string message, ErrorCode errorCode, Exception innnerException) : base(message, innnerException)
        {
            ErrorCode = errorCode;
        }
    }
}
