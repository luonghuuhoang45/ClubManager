using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClubManager.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "Họ tên không được để trống.")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [Display(Name = "Lớp")]
        public string? Class { get; set; }

        [Display(Name = "Chức vụ trong CLB")]
        public string? RoleInClub { get; set; }  // Ví dụ: "Thành viên", "Trưởng CLB", "Thư ký"

        [Display(Name = "Ngày tham gia")]
        public DateTime JoinDate { get; set; } = DateTime.Now;

        [Display(Name = "Ảnh đại diện")]
        public string? AvatarPath { get; set; }

        public ICollection<Membership>? Memberships { get; set; }
        public bool IsActive { get; set; } = true;

    }
}
