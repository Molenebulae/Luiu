namespace Luiu.Domain.Exceptions
{
    public class AppForbiddenException : BaseAppException
    {
        private const string DefaultMessage = "您沒有權限執行此操作";
        public AppForbiddenException(string message = DefaultMessage) : base(null, message, 403) { }
        public AppForbiddenException(object data, string message = DefaultMessage) : base(data, message, 403) { }
    }
}
