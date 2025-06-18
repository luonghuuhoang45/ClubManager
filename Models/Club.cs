using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ClubManager.Models
{
    public class Club
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên CLB là bắt buộc.")]
        [StringLength(100)]
        [Display(Name = "Tên câu lạc bộ")]
        public string Name { get; set; }

        [StringLength(500)]
        [Display(Name = "Mô tả")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Ngày thành lập là bắt buộc.")]
        [Display(Name = "Ngày thành lập")]
        [DataType(DataType.Date)]
        public DateTime FoundedDate { get; set; }

        [BindNever]
        public List<Membership>? Memberships { get; set; }
        public bool IsActive { get; set; } = true;

    }
}
