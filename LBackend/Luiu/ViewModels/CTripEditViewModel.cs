using Luiu.Models;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;



namespace Luiu.ViewModels
{
    public class CTripEditViewModel
    {
        public int TripID { get; set; } // Create 時為 0, Edit 時有值

        [Required(ErrorMessage = "請輸入行程名稱")]
        [Display(Name = "行程名稱")]
        public string TripName { get; set; }

        [Required(ErrorMessage = "日期不可空白")]
        [DataType(DataType.Date)]
        [Display(Name = "開始日期")]
        public DateTime StartDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "請輸入建立者 ID")]
        [Display(Name = "建立者 ID")]
        public int OwnerID { get; set; }

        [Display(Name = "建立者姓名")]
        public string OwnerName { get; set; } // 用於顯示唯讀姓名

        [Display(Name = "選擇行李清單")]
        public int? ListId { get; set; }

        // 用來存儲下拉選單的選項內容
        public SelectList PackingListOptions { get; set; }

        public byte? PrivacyStatus { get; set; }
        public string ShortCode { get; set; }
        public Guid TripGuid { get; set; }
    }
}
