namespace Luiu.Domain.Exceptions
{
    public class AppExternalServiceException : BaseAppException
    {
        private const string DefaultMessage = "外部服務暫時無法使用";
        public AppExternalServiceException(string message = DefaultMessage) : base(null, message, 502) { }
        public AppExternalServiceException(object data, string message = DefaultMessage) : base(data, message, 502) { }
    }
}
