using System.Security;

namespace Luiu.ViewModels
{
    public class CMemberSearchViewModel
    {
        public string Search { get; set; }
        public string Category { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }

        public override string ToString()
        {
            return $"{Search} {Category} {Page} {PageSize} {TotalCount}";
        }
    }
}
