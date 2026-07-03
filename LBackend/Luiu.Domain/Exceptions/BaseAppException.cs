namespace Luiu.Domain.Exceptions
{
    public class BaseAppException : Exception
    {
        public int StatusCode { get; }
        public object? ExtraData { get; }

        public BaseAppException(object? data, string message, int statusCode) : base(message)
        {
            StatusCode = statusCode;
            ExtraData = data;
        }
    }
}
