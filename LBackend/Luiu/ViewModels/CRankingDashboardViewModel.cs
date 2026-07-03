using Luiu.Models;

namespace Luiu.ViewModels
{
    public class CRankingDashboardViewModel
    {
        public CRankingWeight Weights { get; set; }
        public DateTime? ConfigLastUpdated { get; set; }

        public DateTime? LastSnapTime { get; set; }
        public DateTime? LastSpotRankDate { get; set; }
        public DateTime? LastEventRankDate { get; set; }
    }
}
