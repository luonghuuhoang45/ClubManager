using System.ComponentModel.DataAnnotations;

namespace ClubManager.Models
{
    public class Student
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên không được để trống")]
        [StringLength(100, ErrorMessage = "Tên không được vượt quá 100 ký tự")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Ngày tạo")]
        [DataType(DataType.Date)]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Quan hệ nhiều-nhiều: Student <-> Club thông qua Membership
        public List<Membership> Memberships { get; set; } = new();
        public string UserId { get; internal set; }
    }
}
