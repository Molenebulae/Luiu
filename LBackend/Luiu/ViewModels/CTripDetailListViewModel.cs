using Luiu.Models;
using Microsoft.Data.SqlClient;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;



namespace Luiu.ViewModels
{
    public class CTripDetailListViewModel
    {
        private TTripDetail _tripdetail;

        public int DetailID { get; set; }

        public int TripID { get; set; }

        public int SpotID { get; set; }
        [DisplayName("景點別稱")]
        public string SpotAlias { get; set; }
        [DisplayName("小筆記")]
        public string Notes { get; set; }
        [DisplayName("天")]
        public int DayNumber { get; set; }
        [DisplayName("景點編號")]
        public int SortOrder { get; set; }

        [DisplayName("抵達時間")]
        public int ArrivalTime  { get; set; }

        // 2. 顯示與輸入用：將分鐘數轉為 "HH:mm" 格式
        [DisplayName("抵達時間")]
        [DataType(DataType.Time)] // 這會讓 HTML 生成 <input type="time">
        public string ArrivalTimeDisplay
        {
            get
            {
                // 將分鐘數轉為 HH:mm 格式輸出給 View
                int hours = ArrivalTime / 60;
                int minutes = ArrivalTime % 60;
                return $"{hours:D2}:{minutes:D2}";
            }
            set
            {
                // 當從 View 接收到 "14:30" 格式字串時，轉回分鐘數存入 ArrivalTime
                if (TimeSpan.TryParse(value, out TimeSpan ts))
                {
                    ArrivalTime = (int)ts.TotalMinutes;
                }
            }
        }
        [DisplayName("停留時間")]
        public int StayDuration { get; set; }
        [DisplayName("交通方式")]
        public int TransportMode { get; set; }
        [DisplayName("交通時間")]
        public int TransportTime { get; set; }

        [DisplayName("版本編號")]
        public int VersionID { get; set; }
        
        //[DisplayName("標記")]
        public bool IsMaster { get; set; }
        
        [DisplayName("給予建議者")]
        public int SuggestBy { get; set; }

        [DisplayName("建立時間")]
        public DateTime? CreateAt { get; set; }
        
        [DisplayName("最後更新時間")]
        public DateTime? UpdateAt { get; set; }

    }
}
