namespace Luiu.Domain.Exceptions
{
    public class AppTooManyRequestsException : BaseAppException
    {
        private const string DefaultMessage = "您的請求太過頻繁，請稍後再試";
        public AppTooManyRequestsException(string message = DefaultMessage) : base(null, message, 429) { }
        public AppTooManyRequestsException(object data, string message = DefaultMessage) : base(data, message, 429) { }
    }
}
