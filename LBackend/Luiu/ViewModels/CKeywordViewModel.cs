namespace Luiu.ViewModels
{
    public class CKeywordViewModel
    {
        public string txtKeyword { get; set; }
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
