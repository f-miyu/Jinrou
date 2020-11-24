using System;
using Grpc.Core;
using JinrouClient.Domain;

namespace JinrouClient.Data
{
    public static class JinrouExceptionMapper
    {
        public static JinrouException Transform(RpcException exception)
        {
            var errorCode = exception.Status switch
            {
                { StatusCode: StatusCode.Unauthenticated, Detail: "Token is expired" } => ErrorCode.TokenExpired,
                { StatusCode: StatusCode.Unauthenticated } => ErrorCode.Unauthenticated,
                { StatusCode: StatusCode.InvalidArgument } => ErrorCode.Unauthenticated,
                _ => ErrorCode.Unkown,
            };

            return new JinrouException(exception.Status.Detail, errorCode, exception);
        }
    }
}
