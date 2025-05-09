using ClubManager.Data;
using ClubManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClubManager.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ClubsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        public ClubsController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        

        // GET: Clubs
        public async Task<IActionResult> Index()
        {
            // Lấy danh sách câu lạc bộ
            var clubs = await _context.Clubs.ToListAsync();
            return View(clubs);
        }

        // GET: Clubs/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Clubs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,FoundedDate")] Club club)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Đảm bảo rằng không có thành viên trong câu lạc bộ khi tạo
                    club.Memberships = new List<Membership>();

                    // Thêm câu lạc bộ vào cơ sở dữ liệu
                    _context.Add(club);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index)); // Chuyển hướng về danh sách câu lạc bộ
                }
                catch (Exception ex)
                {
                    // Log lỗi nếu có vấn đề khi lưu
                    Console.WriteLine($"Lỗi khi lưu club: {ex.Message}");
                    ModelState.AddModelError("", "Đã xảy ra lỗi khi lưu câu lạc bộ.");
                }
            }

            return View(club);
        }

        // GET: Clubs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var club = await _context.Clubs.FindAsync(id);
            if (club == null) return NotFound();

            return View(club);
        }

        // POST: Clubs/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,FoundedDate")] Club club)
        {
            if (id != club.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Cập nhật thông tin câu lạc bộ
                    _context.Update(club);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Clubs.Any(e => e.Id == club.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(club);
        }

        // GET: Clubs/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var club = await _context.Clubs
                .FirstOrDefaultAsync(m => m.Id == id);
            if (club == null) return NotFound();

            return View(club);
        }

        // POST: Clubs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var club = await _context.Clubs.FindAsync(id);
            if (club != null)
            {
                // Xóa câu lạc bộ khỏi cơ sở dữ liệu
                _context.Clubs.Remove(club);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Clubs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var club = await _context.Clubs
                .Include(c => c.Memberships) // Bao gồm thông tin thành viên
                .ThenInclude(m => m.Student) // Bao gồm thông tin người dùng (Student)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (club == null) return NotFound();

            return View(club);
        }

        // GET: Clubs/Join/5 - Gia nhập câu lạc bộ
        public async Task<IActionResult> Join(int? clubId)
        {
            if (clubId == null) return NotFound();

            var club = await _context.Clubs.FindAsync(clubId);
            if (club == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            // Thêm thành viên vào câu lạc bộ
            var membership = new Membership
            {
                ClubId = club.Id,
                StudentId = int.Parse(user.Id),
                JoinDate = DateTime.Now
            };

            _context.Memberships.Add(membership);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), "Home"); // Sau khi gia nhập, chuyển về trang chủ
        }
    }
}
