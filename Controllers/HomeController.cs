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

            if (user == null)
                return RedirectToAction("Login", "Account");

            ViewBag.UserName = user.FullName;

            var allClubs = await _context.Clubs.ToListAsync();

            var joinedClubIds = await _context.Memberships
                .Where(m => m.ApplicationUserId == user.Id)
                .Select(m => m.ClubId)
                .ToListAsync();

            var model = allClubs.Select(club => new ClubViewModel
            {
                Club = club,
                HasJoined = joinedClubIds.Contains(club.Id)
            }).ToList();

            return View(model);
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
