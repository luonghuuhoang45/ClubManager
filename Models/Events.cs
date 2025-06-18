using System.ComponentModel.DataAnnotations;

namespace ClubManager.Models
{
    public class Event
    {
        public int Id { get; set; }

        [Required, Display(Name = "Tiêu đề")]
        public string Title { get; set; }

        [Display(Name = "Mô tả")]
        public string Description { get; set; }

        [DataType(DataType.DateTime), Display(Name = "Bắt đầu")]
        public DateTime StartTime { get; set; }

        [DataType(DataType.DateTime), Display(Name = "Kết thúc")]
        public DateTime EndTime { get; set; }

        [Display(Name = "Địa điểm")]
        public string Location { get; set; }

        [Display(Name = "Thuộc CLB")]
        public int ClubId { get; set; }
        public Club Club { get; set; }

        [Display(Name = "Người tham gia")]
        public List<EventParticipant> Participants { get; set; } = new();

        public bool IsActive { get; set; } = true;
    }

}
