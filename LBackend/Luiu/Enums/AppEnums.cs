using System.ComponentModel.DataAnnotations;

namespace Luiu.Enums
{
    public static class AppEnums
    {
        /// <summary>
        /// Luiu 專案全域列舉定義中心
        /// </summary>
        public enum MemberStatus : byte
        {
            [Display(Name = "待驗證")]
            Unverified = 0,
            [Display(Name = "正常")]
            Acitve = 1,
            [Display(Name = "停權")]
            Suspended = 2,
            [Display(Name = "審核")]
            Review = 3
        }

        public enum MemberGenday : byte
        {
            [Display(Name = "不透漏")]
            Unspecified = 0,
            [Display(Name = "男")]
            Male = 1,
            [Display(Name = "女")]
            Femle = 2
        }

        public enum RoleType: byte
        {
            [Display(Name = "訪客")]
            Visitors = 0,
            [Display(Name = "一般會員")]
            Member = 1,
            [Display(Name = "一般員工")]
            Employee = 11,
        }
    }
}
