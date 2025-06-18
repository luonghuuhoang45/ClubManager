using System.ComponentModel.DataAnnotations;

namespace ClubManager.Models
{
    public enum MembershipStatus
    {
        Pending,   // Đang chờ duyệt
        Approved,  // Đã được duyệt
        Rejected   // Bị từ chối
    }

    public class Membership
    {
        public int Id { get; set; }

        [Display(Name = "ID sinh viên")]
        public int StudentId { get; set; }

        public Student Student { get; set; }

        [Display(Name = "ID câu lạc bộ")]
        public int ClubId { get; set; }

        public Club Club { get; set; }

        public string ApplicationUserId { get; set; }

        [Display(Name = "Ngày tham gia")]
        [DataType(DataType.Date)]
        public DateTime JoinDate { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        public MembershipStatus Status { get; set; } = MembershipStatus.Pending;
    }
}
