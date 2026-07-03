using System.ComponentModel.DataAnnotations;

namespace Luiu.Models
{
    public class CRankingWeight
    {
        [Range(0, int.MaxValue, ErrorMessage = "權重不可為負數")]
        public int ViewCount { get; set; } = 1;
        [Range(0, int.MaxValue, ErrorMessage = "權重不可為負數")]
        public int FavoriteCount { get; set; } = 1;
        [Range(0, int.MaxValue, ErrorMessage = "權重不可為負數")]
        public int PlanCount { get; set; } = 1;
        [Range(0, int.MaxValue, ErrorMessage = "權重不可為負數")]
        public int RecordCount { get; set; } = 1;
        [Range(0, int.MaxValue, ErrorMessage = "權重不可為負數")]
        public int RefCount { get; set; } = 1;
    }
}
