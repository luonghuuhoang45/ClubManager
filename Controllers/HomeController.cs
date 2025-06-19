using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ClubManager.Models;
using ClubManager.Data;
using Microsoft.EntityFrameworkCore;
using ClubManager.Models.ViewModels; // Ensure this is the correct namespace for ApplicationDbContext
using Microsoft.AspNetCore.Authorization;

namespace ClubManager.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Redirect("/Identity/Account/Login");

            // Lấy tất cả membership của user (kể cả chưa duyệt)
            var userMemberships = await _context.Memberships
                .Where(m => m.ApplicationUserId == user.Id)
                .ToListAsync();

            var joinedClubIds = userMemberships
                .Where(m => m.IsActive)
                .Select(m => m.ClubId)
                .ToList();

            var clubs = await _context.Clubs
                .Where(c => c.IsActive)
                .ToListAsync();

            var clubViewModels = clubs.Select(club =>
            {
                // Lấy membership mới nhất của user cho club này
                var membership = userMemberships
                    .Where(m => m.ClubId == club.Id)
                    .OrderByDescending(m => m.Id)
                    .FirstOrDefault();

                return new ClubViewModel
                {
                    Club = club,
                    HasJoined = membership != null && membership.Status == MembershipStatus.Approved,
                    HasRequested = membership != null && membership.Status == MembershipStatus.Pending
                };
            }).ToList();

            var events = await _context.Events
                .Include(e => e.Club)
                    .ThenInclude(c => c.Memberships)
                .Include(e => e.Participants)
                    .ThenInclude(p => p.Student)
                .Where(e => e.IsActive && e.StartTime > DateTime.Now)
                .ToListAsync();

            // Lấy danh sách ClubId mà user đang active
            var activeClubIds = userMemberships
                .Where(m => m.Status == MembershipStatus.Approved)
                .Select(m => m.ClubId)
                .ToHashSet();

            // Sắp xếp: event thuộc CLB user active lên trước, sau đó theo thời gian bắt đầu
            events = events
                .OrderByDescending(e => activeClubIds.Contains(e.ClubId))
                .ThenBy(e => e.StartTime)
                .ToList();

            // Lấy top students và chỉ tính số CLB đã tham gia (Approved)
            var topStudentsRaw = await _context.Students
                .Where(s => s.IsActive)
                .Include(s => s.Memberships)
                .ToListAsync();

            var topStudents = topStudentsRaw
                .OrderByDescending(s => s.Memberships.Count(m => m.Status == MembershipStatus.Approved))
                .Take(5)
                .ToList();

            var model = new HomeIndexViewModel
            {
                Clubs = clubViewModels,
                Events = events,
                JoinedClubIds = joinedClubIds,

                // Dữ liệu dashboard
                TotalUsers = await _userManager.Users.CountAsync(),
                TotalManagers = await _userManager.GetUsersInRoleAsync("ClubManager").ContinueWith(t => t.Result.Count),
                TotalClubs = await _context.Clubs.CountAsync(),
                TotalEvents = await _context.Events.CountAsync(),

                TopStudents = topStudents
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> JoinClub(int clubId)
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null)
            {
                student = new Student
                {
                    FullName = user.FullName,
                    Email = user.Email,
                    UserId = user.Id,
                    CreatedAt = DateTime.Now
                };
                _context.Students.Add(student);
                await _context.SaveChangesAsync();
            }

            // Lấy membership mới nhất của user với club này (nếu có)
            var existingMembership = await _context.Memberships
                .Where(m => m.ClubId == clubId && m.ApplicationUserId == userId)
                .OrderByDescending(m => m.Id)
                .FirstOrDefaultAsync();

            if (existingMembership != null)
            {
                // Nếu đã từng apply, chỉ cập nhật trạng thái về Pending
                existingMembership.Status = MembershipStatus.Pending;
                existingMembership.IsActive = false;
                existingMembership.JoinDate = DateTime.Now;
                await _context.SaveChangesAsync();
            }
            else
            {
                // Chưa từng xin gia nhập, tạo mới
                var membership = new Membership
                {
                    ClubId = clubId,
                    StudentId = student.Id,
                    ApplicationUserId = userId,
                    Status = MembershipStatus.Pending,
                    IsActive = false, // Đảm bảo luôn là false khi chờ duyệt
                    JoinDate = DateTime.Now
                };
                _context.Memberships.Add(membership);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index", "Home");
        }

        // Hiển thị danh sách yêu cầu xét duyệt membership cho admin/manager
        [Authorize(Roles = "Admin,ClubManager")]
        public async Task<IActionResult> PendingMemberships()
        {
            var pendingMemberships = await _context.Memberships
                .Include(m => m.Student)
                .Include(m => m.Club)
                .Where(m => m.Status == MembershipStatus.Pending && (m.IsActive == false || m.IsActive == null))
                .ToListAsync();

            return View(pendingMemberships);
        }

        // Hành động xem yêu cầu xét duyệt cho từng CLB (dùng cho ClubManager)
        [Authorize(Roles = "Admin,ClubManager")]
        public async Task<IActionResult> MembershipRequests(int clubId)
        {
            var requests = await _context.Memberships
                .Include(m => m.Student)
                .Where(m => m.ClubId == clubId && m.Status == MembershipStatus.Pending && (m.IsActive == false || m.IsActive == null))
                .ToListAsync();

            ViewBag.ClubId = clubId;
            return View(requests);
        }

    }

}

