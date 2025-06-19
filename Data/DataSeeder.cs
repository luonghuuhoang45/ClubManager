using ClubManager.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClubManager.Data
{
    public static class DataSeeder
    {
        public static async Task SeedRoles(RoleManager<IdentityRole> roleManager)
        {
            string[] roles = { "Admin", "ClubManager", "Member" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        public static async Task SeedUsers(UserManager<ApplicationUser> userManager, AppDbContext context)
        {
            var adminEmail = "admin@demo.com";
            var managerEmail = "manager@demo.com";
            var memberEmail = "member@demo.com";

            // ========== 1. Admin ==========
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Quản trị viên",
                    EmailConfirmed = true,
                    IsActive = true
                };
                await userManager.CreateAsync(admin, "Admin123!");
                await userManager.AddToRoleAsync(admin, "Admin");
            }

            // ========== 2. Manager ==========
            var managerUser = await userManager.FindByEmailAsync(managerEmail);
            if (managerUser == null)
            {
                managerUser = new ApplicationUser
                {
                    UserName = managerEmail,
                    Email = managerEmail,
                    FullName = "Quản lý CLB CNTT",
                    EmailConfirmed = true,
                    IsActive = true
                };
                await userManager.CreateAsync(managerUser, "Manager123!");
                await userManager.AddToRoleAsync(managerUser, "ClubManager");
            }

            // Tạo student tương ứng với manager nếu chưa có
            var managerStudent = await context.Students.FirstOrDefaultAsync(s => s.UserId == managerUser.Id);
            if (managerStudent == null)
            {
                managerStudent = new Student
                {
                    FullName = managerUser.FullName,
                    Email = managerUser.Email,
                    UserId = managerUser.Id,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };
                context.Students.Add(managerStudent);
                await context.SaveChangesAsync();
            }

            // ========== 3. Club ==========
            var club = await context.Clubs.FirstOrDefaultAsync(c => c.Name == "CLB CNTT");
            if (club == null)
            {
                club = new Club
                {
                    Name = "CLB CNTT",
                    Description = "CLB dành cho sinh viên yêu thích công nghệ",
                    FoundedDate = new DateTime(2022, 1, 1),
                    IsActive = true
                };
                context.Clubs.Add(club);
                await context.SaveChangesAsync();
            }

            // ========== 4. Membership ==========
            bool hasMembership = await context.Memberships.AnyAsync(m =>
                m.StudentId == managerStudent.Id && m.ClubId == club.Id);

            if (!hasMembership)
            {
                context.Memberships.Add(new Membership
                {
                    StudentId = managerStudent.Id,
                    ClubId = club.Id,
                    ApplicationUserId = managerUser.Id,
                    JoinDate = DateTime.Now,
                    Status = MembershipStatus.Approved,
                    IsActive = true
                });
                await context.SaveChangesAsync();
            }

            // ========== 5. Event ==========
            var existingEvent = await context.Events.FirstOrDefaultAsync(e => e.Title == "Hội thảo AI 2025");
            if (existingEvent == null)
            {
                existingEvent = new Event
                {
                    Title = "Hội thảo AI 2025",
                    Description = "Chia sẻ kiến thức về AI và machine learning",
                    ClubId = club.Id,
                    StartTime = DateTime.Now.AddDays(2),
                    EndTime = DateTime.Now.AddDays(2).AddHours(2),
                    Location = "Phòng 101, Cơ sở chính",
                    IsActive = true
                };
                context.Events.Add(existingEvent);
                await context.SaveChangesAsync();
            }

            // ========== 6. Tham gia sự kiện ==========
            var isParticipating = await context.EventParticipants.AnyAsync(ep =>
                ep.EventId == existingEvent.Id && ep.StudentId == managerStudent.Id);

            if (!isParticipating)
            {
                context.EventParticipants.Add(new EventParticipant
                {
                    EventId = existingEvent.Id,
                    StudentId = managerStudent.Id,
                    JoinDate = DateTime.Now,
                    IsActive = true
                });
                await context.SaveChangesAsync();
            }

            // ========== 7. Member ==========
            if (await userManager.FindByEmailAsync(memberEmail) == null)
            {
                var member = new ApplicationUser
                {
                    UserName = memberEmail,
                    Email = memberEmail,
                    FullName = "Thành viên",
                    EmailConfirmed = true,
                    IsActive = true
                };
                await userManager.CreateAsync(member, "Member123!");
                await userManager.AddToRoleAsync(member, "Member");
            }

            // ===== Thêm 3 user demo =====
            var demoUsers = new[]
            {
                new { FullName = "Nguyễn Văn A", Email = "member1@club.com", UserName = "member1@club.com" },
                new { FullName = "Lê Thị B", Email = "member2@club.com", UserName = "member2@club.com" },
                new { FullName = "Lý Quang C", Email = "member3@club.com", UserName = "member3@club.com" }
            };

            foreach (var u in demoUsers)
            {
                if (await userManager.FindByEmailAsync(u.Email) == null)
                {
                    var user = new ApplicationUser
                    {
                        UserName = u.UserName,
                        Email = u.Email,
                        FullName = u.FullName,
                        EmailConfirmed = true,
                        IsActive = true
                    };
                    await userManager.CreateAsync(user, "Pass123!");
                    await userManager.AddToRoleAsync(user, "Member");
                }
            }

            // ===== Thêm 3 CLB demo =====
            if (!context.Clubs.Any(c => c.Name == "CLB Bóng Đá Sao Vàng"))
            {
                context.Clubs.Add(new Club
                {
                    Name = "CLB Bóng Đá Sao Vàng",
                    Description = "Nơi giao lưu, rèn luyện và thi đấu bóng đá.",
                    FoundedDate = new DateTime(2018, 5, 10),
                    IsActive = true
                });
            }
            if (!context.Clubs.Any(c => c.Name == "CLB Sách & Tri Thức"))
            {
                context.Clubs.Add(new Club
                {
                    Name = "CLB Sách & Tri Thức",
                    Description = "Cộng đồng yêu sách, chia sẻ kiến thức và kỹ năng đọc.",
                    FoundedDate = new DateTime(2020, 9, 1),
                    IsActive = true
                });
            }
            if (!context.Clubs.Any(c => c.Name == "CLB Công Nghệ Trẻ"))
            {
                context.Clubs.Add(new Club
                {
                    Name = "CLB Công Nghệ Trẻ",
                    Description = "Nơi học hỏi, sáng tạo và phát triển các dự án công nghệ.",
                    FoundedDate = new DateTime(2019, 3, 15),
                    IsActive = true
                });
            }
            await context.SaveChangesAsync();

            // ===== Thêm 5 sự kiện demo =====
            var clubList = context.Clubs.Take(3).ToList();
            if (clubList.Count == 3)
            {
                if (!context.Events.Any(e => e.Title == "Giải Bóng Đá Sinh Viên 2025"))
                {
                    context.Events.Add(new Event
                    {
                        Title = "Giải Bóng Đá Sinh Viên 2025",
                        Description = "Giải đấu bóng đá dành cho sinh viên toàn trường.",
                        StartTime = DateTime.Now.AddDays(7),
                        EndTime = DateTime.Now.AddDays(8),
                        Location = "Sân vận động A",
                        ClubId = clubList[0].Id,
                        IsActive = true
                    });
                }
                if (!context.Events.Any(e => e.Title == "Ngày Hội Đọc Sách"))
                {
                    context.Events.Add(new Event
                    {
                        Title = "Ngày Hội Đọc Sách",
                        Description = "Chia sẻ sách hay, giao lưu tác giả, tặng sách miễn phí.",
                        StartTime = DateTime.Now.AddDays(10),
                        EndTime = DateTime.Now.AddDays(10).AddHours(4),
                        Location = "Thư viện trung tâm",
                        ClubId = clubList[1].Id,
                        IsActive = true
                    });
                }
                if (!context.Events.Any(e => e.Title == "Workshop Lập Trình Web"))
                {
                    context.Events.Add(new Event
                    {
                        Title = "Workshop Lập Trình Web",
                        Description = "Học lập trình web cơ bản cho sinh viên CNTT.",
                        StartTime = DateTime.Now.AddDays(14),
                        EndTime = DateTime.Now.AddDays(14).AddHours(3),
                        Location = "Phòng Lab 2",
                        ClubId = clubList[2].Id,
                        IsActive = true
                    });
                }
                if (!context.Events.Any(e => e.Title == "Chung Kết Bóng Đá"))
                {
                    context.Events.Add(new Event
                    {
                        Title = "Chung Kết Bóng Đá",
                        Description = "Trận chung kết giải bóng đá sinh viên.",
                        StartTime = DateTime.Now.AddDays(15),
                        EndTime = DateTime.Now.AddDays(15).AddHours(2),
                        Location = "Sân vận động A",
                        ClubId = clubList[0].Id,
                        IsActive = true
                    });
                }
                if (!context.Events.Any(e => e.Title == "Talkshow Công Nghệ 2025"))
                {
                    context.Events.Add(new Event
                    {
                        Title = "Talkshow Công Nghệ 2025",
                        Description = "Giao lưu với các chuyên gia công nghệ trẻ.",
                        StartTime = DateTime.Now.AddDays(20),
                        EndTime = DateTime.Now.AddDays(20).AddHours(2),
                        Location = "Hội trường lớn",
                        ClubId = clubList[2].Id,
                        IsActive = true
                    });
                }
                await context.SaveChangesAsync();
            }
        }
    }
}
