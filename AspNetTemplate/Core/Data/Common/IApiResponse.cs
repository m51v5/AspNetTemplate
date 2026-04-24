using System.Net;
using System.Text.RegularExpressions;

namespace AspNetTemplate.Core.Data.Common
{
    public interface IApiResponse
    {
        public bool IsSuccess => false;
        HttpStatusCode StatusCode { get; }
    }

    public class FailureResponse : IApiResponse
    {
        public bool IsSuccess => false;
        public HttpStatusCode StatusCode { get; set; }
        public string? ErrorMessage { get; set; }

        public FailureResponse(string? errorMessage, HttpStatusCode statusCode, object? metaData = null)
        {
            StatusCode = statusCode;
            ErrorMessage = errorMessage ?? Regex.Replace(statusCode.ToString(), "(\\B[A-Z])", " $1");
        }

        public FailureResponse(HttpStatusCode statusCode, object? metaData = null)
        {
            StatusCode = statusCode;
            ErrorMessage = Regex.Replace(statusCode.ToString(), "(\\B[A-Z])", " $1");
        }
    }

    public class SuccessResponse<T>(T data, HttpStatusCode statusCode = HttpStatusCode.OK, object? metaData = null) : IApiResponse
    {
        public bool IsSuccess => true;
        public HttpStatusCode StatusCode { get; set; } = statusCode;
        public T? Data { get; set; } = data;
        public object? MetaData { get; set; } = metaData;
    }
    
    public class EmptySuccessResponse(HttpStatusCode statusCode = HttpStatusCode.OK, object? metaData = null) : IApiResponse
    {
        public bool IsSuccess => true;
        public HttpStatusCode StatusCode { get; set; } = statusCode;
        public object? Data { get; set; } = null;
        public object? MetaData { get; set; } = metaData;
    }
}