using Luiu.Models;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;



namespace Luiu.ViewModels
{
    public class CTripListViewModel
    {
        private TTrip _trip;
        public int TripID { get; set; }
        [DisplayName("行程名稱")]
        [Required(ErrorMessage = "名稱不可空白")]
        public string TripName { get; set; }

        [DisplayName("開始日期")]
        [Required(ErrorMessage = "日期不可空白")]

        public string StartDate { get; set; }
        [DisplayName("結束日期")]
        [Required(ErrorMessage = "日期不可空白")]

        public string EndDate { get; set; }

        [DisplayName("建立時間")]
        public DateTime? CreateAt { get; set; }
        [DisplayName("最後更新時間")]
        public DateTime? UpdateAt { get; set; }
        [DisplayName("是否精選")]
        public bool IsSuggest { get; set; }
        [DisplayName("是否公開")]
        public byte? PrivacyStatus { get; set; }
        // 新增一個輔助屬性供 Checkbox 使用
        public bool IsPublic => PrivacyStatus == 1;

        [DisplayName("建立者")]
        public string OwnerName { get; set; }
        // 不存在trip表內但需要能被搜尋及顯示

        public int OwnerId { get; set; }

        public string? OwnerIcon { get; set; }
        public int Page { get; set; } = 1;      // 當前頁碼
        public int PageSize { get; set; } = 10; // 每頁幾筆
        public int TotalCount { get; set; }     // 總資料筆數

        public string Search { get; set; }
        public string Category { get; set; }

        public override string ToString()
        {
            return $"{Search} {Category} {Page} {PageSize} {TotalCount}";
        }
    }
}
