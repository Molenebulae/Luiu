using Luiu.Models;

namespace Luiu.ViewModels
{
    public class CRankingHistoryViewModel
    {
        public string SelectedRankType { get; set; } // 格式 "2026-03" 或 "2026-year"
        public List<string> AvailableRankTypes { get; set; }

        public List<TSpotRank> SpotRanks { get; set; }
        public List<TRegionRank> RegionRanks { get; set; }
        public List<TEventRank> EventRanks { get; set; }
    }

}
