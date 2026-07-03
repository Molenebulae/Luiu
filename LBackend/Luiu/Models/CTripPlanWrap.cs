using System.ComponentModel;
using System.ComponentModel.DataAnnotations;





namespace Luiu.Models
{
    public class CTripPlanWrap
    {
        private TTrip _trip;
        public TTrip Trip
        {
            get { return _trip; }
            set { _trip = value; }
        }

        public CTripPlanWrap()
        {
            _trip = new TTrip();
        }

        public int TripId
        {
            get { return _trip.TripId; }
            set { _trip.TripId = value; }
        }

        [DisplayName("規劃名稱")]
        [Required(ErrorMessage = "名稱不可空白")]
        public string TripName
        {
            get { return _trip.TripName; }
            set { _trip.TripName = value; }
        }

        [DisplayName("開始日期")]
        [Required(ErrorMessage = "日期不可空白")]
        public DateOnly StartDate
        {
            get { return _trip.StartDate; }
            set { _trip.StartDate = value; }
        }

        [DisplayName("結束日期")]
        [Required(ErrorMessage = "日期不可空白")]
        public DateOnly EndDate
        {
            get { return _trip.EndDate; }
            set { _trip.EndDate = value; }
        }

        [DisplayName("是否公開")]
        public byte? PrivacyStatus
        {
            get { return _trip.PrivacyStatus; }
            set { _trip.PrivacyStatus = value; }
        }

        [DisplayName("是否精選")]
        public bool? IsSuggest
        {
            get { return _trip.IsSuggest; }
            set { _trip.IsSuggest = value; }
        }

        public Guid? TripGuid
        {
            get { return _trip.TripGuid; }
            set { _trip.TripGuid = value; }
        }

        public string? ShortCode
        {
            get { return _trip.ShortCode; }
            set { _trip.ShortCode = value; }
        }


        public int OwnerId
        {
            get { return _trip.OwnerId; }
            set { _trip.OwnerId = value; }
        }

        [DisplayName("行李清單選用")]
        public int? ListId
        {
            get { return _trip.ListId; }
            set { _trip.ListId = value; }
        }

        public bool IsDeleted
        {
            get { return _trip.IsDeleted; }
            set { _trip.IsDeleted = value; }
        }

        [DisplayName("建立日期")]
        public DateTime CreateAt
        {
            get { return _trip.CreateAt; }
            set { _trip.CreateAt = value; }
        }

        [DisplayName("最後更新日期")]
        public DateTime? UpdateAt
        {
            get { return _trip.UpdateAt; }
            set { _trip.UpdateAt = value; }
        }


        // 導覽屬性
        [DisplayName("建立者名稱")]
        public string? OwnerName { get; set; }
        public string? OwnerIcon { get; set; }
    }
}
