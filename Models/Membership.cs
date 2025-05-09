using System.ComponentModel.DataAnnotations;

namespace ClubManager.Models
{
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
    }
}
