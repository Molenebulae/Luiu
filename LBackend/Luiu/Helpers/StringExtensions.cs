namespace Luiu.Helpers
{
    public static class StringExtensions
    {
        public static string ToPhoneFormat(this string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return "未填";
            if (phone.Length == 10)
                return System.Text.RegularExpressions.Regex.Replace(phone, @"(\d{4})(\d{3})(\d{3})", "$1-$2-$3");
            return phone;
        }
    }
}
