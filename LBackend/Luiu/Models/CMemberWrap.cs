using Microsoft.Identity.Client;
using Luiu.Enums;
using Microsoft.AspNetCore.CookiePolicy;
using System.ComponentModel.DataAnnotations;

namespace Luiu.Models
{
    public class CMemberWrap : BaseModel
    {
        private TMember _member;

        public CMemberWrap() => _member = new TMember();
        public TMember member
        {
            get { return _member; }
            set { _member = value; }
        }

        public int MemberId
        {
            get { return _member.MemberId; }
            set { _member.MemberId = value; }
        }

        public string UserId
        {
            get { return _member.UserId; }
            set { _member.UserId = value; }
        }
        [Display(Name = "郵件")]
        public string Email
        {
            get { return _member.Email; }
            set { _member.Email = value; }
        }
        [Display(Name = "密碼")]
        public string Password
        {
            get { return _member.Password; }
            set { _member.Password = value; }
        }
        [Display(Name = "名字")]
        public string Name
        {
            get { return _member.Name; }
            set { _member.Name = value; }
        }
        [Display(Name = "電話")]
        [DisplayFormat(NullDisplayText = "未填")]
        public string? Phone
        {
            get { return _member.Phone; }
            set { _member.Phone = value; }
        }
        [Display(Name = "生日")]
        [DisplayFormat(DataFormatString = "{0:yyyy/MM/dd}", NullDisplayText = "未填", ApplyFormatInEditMode = true)]
        public DateOnly? Birthday
        {
            get { return _member.Birthday; }
            set { _member.Birthday = value; }
        }
        [Display(Name = "性別")]
        public AppEnums.MemberGenday Gender
        {
            get { return (AppEnums.MemberGenday)_member.Gender; }
            set { _member.Gender = (byte)value; }
        }

        public string? IconUrl
        {
            get { return _member.IconUrl; }
            set { _member.IconUrl = value; }
        }
        public IFormFile Icon{ get; set; }
        public string DisplayIconUrl
        {
            get
            {
                if (string.IsNullOrEmpty(IconUrl))
                    return "/Images/default.png";

                if (IconUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    return IconUrl;
                }
                return "/Images/" + IconUrl;
            }
        }

        [Display(Name = "註冊時間")]
        [DisplayFormat(DataFormatString = "{0:yyyy/MM/dd HH:mm}", ApplyFormatInEditMode = true)]
        public DateTime CreateDate
        {
            get { return _member.CreateDate; }
            set { _member.CreateDate = value; }
        }
        [Display(Name = "更新時間")]
        public DateTime UpdateDate
        {
            get { return _member.UpdateDate; }
            set { _member.UpdateDate = value; }
        }
        [Display(Name = "隱私設定")]
        public bool ProfileStatus
        {
            get { return _member.ProfileStatus; }
            set { _member.ProfileStatus = value; }
        }
        [Display(Name = "帳號狀態")]
        public AppEnums.MemberStatus Status
        {
            get 
            {
                //if (Enum.IsDefined(typeof(AppEnums.MemberStatus), (int)_member.Status))
                //{
                //}
                //return AppEnums.MemberStatus.Review;
                return (AppEnums.MemberStatus)_member.Status;
            }
            set { _member.Status = (byte)value; }
        }
        [Display(Name = "停權原因")]
        public string? LockReason
        {
            get { return _member.LockReason; }
            set { _member.LockReason = value; }
        }

        public bool IsDelete
        {
            get { return _member.IsDelete; }
            set { _member.IsDelete = value; }
        }

        public DateTime? DeleteTime
        {
            get { return _member.DeleteTime; }
            set { _member.DeleteTime = value; }
        }
        [Display(Name = "存取權限")]
        public int RoleId
        {
            get { return _member.RoleId; }
            set { _member.RoleId = value; }
        }

        [Display(Name = "上次登入")]
        [DisplayFormat(DataFormatString = "{0:yyyy/MM/dd HH:mm}", NullDisplayText = "從未登入")]
        public DateTime? LastLoginTime { get; set; }

    }
}
