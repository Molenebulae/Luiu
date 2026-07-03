namespace Luiu.Domain.Exceptions
{
    public class AppNotFoundException : BaseAppException
    {
        private const string DefaultMessage = "找不到指定資源";
        public AppNotFoundException(string message = DefaultMessage) : base(null, message, 404) { }
        public AppNotFoundException(object data, string message = DefaultMessage) : base(data, message, 404) { }
    }
}
