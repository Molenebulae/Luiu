namespace Luiu.Models
{
    public partial class TComment
    {
        public virtual TPost Post { get; set; }
        public virtual TMember Member { get; set; }
    }
}
