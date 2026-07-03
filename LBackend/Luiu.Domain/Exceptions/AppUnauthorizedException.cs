namespace Luiu.Domain.Exceptions
{
    public class AppUnauthorizedException : BaseAppException
    {
        private const string DefaultMessage = "請先登入";
        public AppUnauthorizedException(string message = DefaultMessage) : base(null, message, 401) { }
        public AppUnauthorizedException(object data, string message = DefaultMessage) : base(data, message, 401) { }
    }
}
