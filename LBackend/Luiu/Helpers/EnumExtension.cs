using Luiu.Enums;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Luiu.Helpers
{
    public static class EnumExtension
    {
        public static string GetDisplayName(this Enum enumValue)
        {
            return enumValue.GetType()
                .GetField(enumValue.ToString())
                ?.GetCustomAttribute<DisplayAttribute>()
                ?.Name ?? enumValue.ToString();
        }

        public static string GetBadgeClass(this AppEnums.MemberStatus status)
        {
            return status switch
            {
                AppEnums.MemberStatus.Acitve => "bg-light-success text-success",
                AppEnums.MemberStatus.Suspended => "bt-light-danger text-danger",
                AppEnums.MemberStatus.Review => "bt-light-warning text-warning",
                _ => "bg-light-secondary text-secondary"
            };
        }
    }
}
