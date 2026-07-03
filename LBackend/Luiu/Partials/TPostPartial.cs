namespace Luiu.Models
{
    public partial class TPost
    {
        public TCategory Category { get; set; }
        public TMember Member { get; set; }
        public virtual ICollection<TPhoto> Photos { get; set; } = new List<TPhoto>();
        public virtual ICollection<TComment> Comments { get; set; } = new List<TComment>();
    }
}
