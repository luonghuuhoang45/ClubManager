using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ClubManager.Models;
using ClubManager.Data;
using Microsoft.EntityFrameworkCore;
using ClubManager.Models.ViewModels; // Ensure this is the correct namespace for ApplicationDbContext

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
            var viewModel = new HomeIndexViewModel();

            var allClubs = await _context.Clubs.Where(c => c.IsActive).ToListAsync();
            var allEvents = await _context.Events.Include(e => e.Club).Where(e => e.IsActive).ToListAsync();

            var joinedClubIds = new List<int>();
            var clubVMs = new List<ClubViewModel>();

            if (user != null)
            {
                var memberships = await _context.Memberships
                    .Where(m => m.ApplicationUserId == user.Id && m.IsActive)
                    .ToListAsync();

                joinedClubIds = memberships
                    .Where(m => m.Status == MembershipStatus.Approved)
                    .Select(m => m.ClubId)
                    .ToList();

                clubVMs = allClubs.Select(club => new ClubViewModel
                {
                    Club = club,
                    HasJoined = memberships.Any(m => m.ClubId == club.Id && m.Status == MembershipStatus.Approved),
                    HasRequested = memberships.Any(m => m.ClubId == club.Id && m.Status == MembershipStatus.Pending)
                }).ToList();
            }
            else
            {
                clubVMs = allClubs.Select(club => new ClubViewModel
                {
                    Club = club,
                    HasJoined = false,
                    HasRequested = false
                }).ToList();
            }

            viewModel.Clubs = clubVMs;
            viewModel.Events = allEvents;
            viewModel.JoinedClubIds = joinedClubIds;

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> JoinClub(int clubId)
        {
            var userId = _userManager.GetUserId(User); // Lấy UserId của người dùng hiện tại
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == userId); // Kiểm tra xem Student có tồn tại chưa

            if (student == null)
            {
                // Tạo mới Student nếu chưa có
                student = new Student
                {
                    FullName = user.FullName,  // Giả sử có thuộc tính FullName trong ApplicationUser
                    Email = user.Email,
                    UserId = user.Id,
                    CreatedAt = DateTime.Now
                };
                _context.Students.Add(student);
                await _context.SaveChangesAsync();
            }

            var membership = new Membership
            {
                ClubId = clubId,
                StudentId = student.Id,
                ApplicationUserId = userId // Gắn ApplicationUserId
            };

            _context.Memberships.Add(membership);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Home");

        }


    }

}
