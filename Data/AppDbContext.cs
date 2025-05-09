using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ClubManager.Models;

namespace ClubManager.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Các bảng trong cơ sở dữ liệu
        public DbSet<Event> Events { get; set; }
        public DbSet<Club> Clubs { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Membership> Memberships { get; set; }

        // Cấu hình các mối quan hệ và đặc tính của các bảng
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Cấu hình khóa chính và quan hệ giữa các bảng

            // Cấu hình Membership làm bảng trung gian giữa Student và Club
            builder.Entity<Membership>()
                .HasKey(m => m.Id); // Khóa chính của bảng Membership

            // Cấu hình quan hệ giữa Membership và Student
            builder.Entity<Membership>()
                .HasOne(m => m.Student)
                .WithMany(s => s.Memberships) // Một student có thể tham gia nhiều câu lạc bộ
                .HasForeignKey(m => m.StudentId); // Khóa ngoại trỏ đến Student

            // Cấu hình quan hệ giữa Membership và Club
            builder.Entity<Membership>()
                .HasOne(m => m.Club)
                .WithMany(c => c.Memberships) // Một câu lạc bộ có thể có nhiều thành viên
                .HasForeignKey(m => m.ClubId); // Khóa ngoại trỏ đến Club

            // Cấu hình một số thuộc tính nếu cần thiết
            builder.Entity<Club>()
                .Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(100); // Câu lạc bộ phải có tên và có độ dài tối đa là 100

            builder.Entity<Student>()
                .Property(s => s.FullName)
                .IsRequired()
                .HasMaxLength(100); // Sinh viên phải có tên và có độ dài tối đa là 100

        }
    }
}
