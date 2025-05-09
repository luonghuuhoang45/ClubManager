using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;


namespace ClubManager.Models
{
    public class Club
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên CLB là bắt buộc.")]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Required(ErrorMessage = "Ngày thành lập là bắt buộc.")]
        public DateTime FoundedDate { get; set; }

        [BindNever]
        public List<Membership>? Memberships { get; set; }
    }

}
