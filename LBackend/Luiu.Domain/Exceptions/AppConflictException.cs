namespace Luiu.Domain.Exceptions
{
    public class AppConflictException : BaseAppException
    {
        private const string DefaultMessage = "資料已存在或發生衝突";
        public AppConflictException(string message = DefaultMessage) : base(null, message, 409) { }

        public AppConflictException(object data, string message = DefaultMessage) : base(data, message, 409) { }

        // 可以使用靜態工廠來實作容易重複的錯誤
        public static AppConflictException EmailDuplicate(string email) => new AppConflictException(new { email }, "此 Email 已註冊過");
    }
}
