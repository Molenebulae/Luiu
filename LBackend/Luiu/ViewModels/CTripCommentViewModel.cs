using Luiu.Models;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Luiu.ViewModels
{
    public class CTripCommentViewModel
    {
        private TTripComment _tripcomment;

        public int TripID { get; set; }
        [DisplayName("行程名稱")]
        public string TripName { get; set; }

        public int CommentID { get; set; }

        [DisplayName("留言內容")]
        public string Content { get; set; }

        public int? ParentID { get; set; }

        // 不存在trip表內但需要能被搜尋及顯示
        [DisplayName("留言者")]
        public string UserName { get; set; }

        public int UserID { get; set; }

        public string? UserIcon { get; set; }

        [DisplayName("留言時間")]
        public DateTime? CreateAt { get; set; }

        public bool IsDeleted { get; set; }

    }
}
