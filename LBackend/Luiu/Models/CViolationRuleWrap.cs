using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;

namespace Luiu.Models
{
    public class CViolationRuleWrap : BaseModel
    {
        private TViolationRule _rule;

        public CViolationRuleWrap() => _rule = new TViolationRule();
        public TViolationRule rule
        {
            get { return _rule; }
            set { _rule = value; }
        }

        public int RuleId
        {
            get { return _rule.RuleId; }
            set { _rule.RuleId = value; }
        }

        [Display(Name = "規則名稱")]
        [Required(ErrorMessage = "名稱不可空白")]
        public string Title
        {
            get { return _rule.Title; }
            set { _rule.Title = value; }
        }

        [Display(Name = "說明")]
        [Required(ErrorMessage = "說明不可空白")]
        public string Description
        {
            get { return _rule.Description; }
            set { _rule.Description = value; }
        }

        [Display(Name = "分類")]
        public string RuleType
        {
            get { return _rule.RuleType; }
            set { _rule.RuleType = value; }
        }

        [Display(Name = "狀態")]
        public bool IsActivate
        {
            get { return _rule.IsActivate; }
            set { _rule.IsActivate = value; }
        }

        [Display(Name = "嚴重程度")]
        public byte Severity
        {
            get { return _rule.Severity; }
            set { _rule.Severity = value; }
        }

        [Display(Name = "處分建議")]
        public string? DefaultAction
        {
            get { return _rule.DefaultAction; }
            set { _rule.DefaultAction = value; }
        }

        [Display(Name = "更新時間")]
        [DisplayFormat(DataFormatString = "{0:yyyy/MM/dd HH:mm:ss}")]
        public DateTime UpdatedAt
        {
            get { return _rule.UpdatedAt; }
            set { _rule.UpdatedAt = value; }
        }
    }
}
