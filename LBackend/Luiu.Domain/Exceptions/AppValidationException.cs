namespace Luiu.Domain.Exceptions
{
    public class AppValidationException : BaseAppException
    {
        private const string DefaultMessage = "欄位驗證失敗";
        public AppValidationException(string message = DefaultMessage) : base(null, message, 422) { }
        public AppValidationException(object data, string message = DefaultMessage) : base(data, message, 422) { }
    }
}
