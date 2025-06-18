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
        }
    }
}
