namespace Luiu.ViewModels
{
    public class MemoryDetailViewModel
    {
        public int MemoryID { get; set; }
        public string Title { get; set; }
        public string AuthorName { get; set; }
        public string DateRange { get; set; } // 出發與結束日期
        public string CoverImage { get; set; } // 封面圖網址

        // 用來裝每天的行程，例如 "第 1 天：駁二特區 ➡️ 西子灣"
        public List<string> Itinerary { get; set; } = new List<string>();
        public int ReviewStatus { get; set; }
    }
}
