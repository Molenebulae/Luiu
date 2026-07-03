using System.ComponentModel.DataAnnotations;

namespace Luiu.Domain.Enums
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
            /// <summary>
            /// 官方管理員（具備推薦收編與下架特權）
            /// </summary>
            [Display(Name = "內容管理員")]
            OfficialManager = 12
        }

        public enum VerificationType : byte
        {
            Register = 1,
            ResetPassword = 2
        }
        public enum VerificationStatus: byte
        {
            Pending = 0,
            Success = 1,
            Failed = 2,
            Expired = 3,
        }

        public enum CollectType: byte
        {
            Memory = 5,
            Plan = 6,
            Spot = 7
        }
        public enum OfficeOperStatus : short
        {
            None = 0,        // 無操作
            Recommended = 1, // 官方推薦
            Audited = 2,     // 已審核
            Violated = 3     // 違規隱藏
        }
    }
}
