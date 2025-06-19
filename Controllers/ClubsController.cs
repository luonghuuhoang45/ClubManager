using ClubManager.Data;
using ClubManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClubManager.Controllers
{
    [Authorize] // Cho phép tất cả role truy cập controller này
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
            var user = await _userManager.GetUserAsync(User);

            if (User.IsInRole("Admin"))
            {
                var clubs = await _context.Clubs.ToListAsync();
                return View(clubs);
            }

            if (User.IsInRole("ClubManager"))
            {
                var clubIds = await _context.Memberships
                    .Where(m => m.ApplicationUserId == user.Id)
                    .Select(m => m.ClubId)
                    .ToListAsync();

                var clubs = await _context.Clubs
                    .Where(c => clubIds.Contains(c.Id))
                    .ToListAsync();

                return View(clubs);
            }

            // Cho phép Member xem các CLB đã tham gia (active)
            if (User.IsInRole("Member"))
            {
                var clubIds = await _context.Memberships
                    .Where(m => m.ApplicationUserId == user.Id && m.Status == MembershipStatus.Approved && m.IsActive)
                    .Select(m => m.ClubId)
                    .ToListAsync();

                var clubs = await _context.Clubs
                    .Where(c => clubIds.Contains(c.Id))
                    .ToListAsync();

                return View(clubs);
            }

            return Forbid();
        }

        // GET: Clubs/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Clubs/Create
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,FoundedDate")] Club club)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    club.Memberships = new List<Membership>();
                    _context.Add(club);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi khi tạo câu lạc bộ: {ex.Message}");
                    ModelState.AddModelError("", "Đã xảy ra lỗi khi tạo câu lạc bộ.");
                }
            }
            return View(club);
        }

        // GET: Clubs/Edit/5
        [Authorize(Roles = "Admin,ClubManager")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var club = await _context.Clubs.FindAsync(id);
            if (club == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);

            if (User.IsInRole("Admin"))
                return View(club);

            if (User.IsInRole("ClubManager"))
            {
                var isManager = await _context.Memberships
                    .AnyAsync(m => m.ClubId == club.Id && m.ApplicationUserId == user.Id);

                if (isManager)
                    return View(club);
            }

            return Forbid();
        }

        // POST: Clubs/Edit/5
        [Authorize(Roles = "Admin,ClubManager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,FoundedDate")] Club club)
        {
            if (id != club.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(club);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Clubs.Any(c => c.Id == club.Id))
                        return NotFound();
                    throw;
                }
            }

            return View(club);
        }

        // GET: Clubs/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var club = await _context.Clubs.FirstOrDefaultAsync(c => c.Id == id);
            if (club == null) return NotFound();

            return View(club);
        }

        // POST: Clubs/Delete/5
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var club = await _context.Clubs.FindAsync(id);
            if (club != null)
            {
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
                .Include(c => c.Memberships)
                    .ThenInclude(m => m.Student)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (club == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);

            // Nếu là Admin hoặc ClubManager → cho vào
            if (User.IsInRole("Admin") || User.IsInRole("ClubManager"))
                return View(club);

            // Nếu là Member và là thành viên active của CLB đó → cho xem
            var isActiveMember = await _context.Memberships
                .AnyAsync(m => m.ClubId == club.Id && m.ApplicationUserId == user.Id && m.Status == MembershipStatus.Approved && m.IsActive);

            if (User.IsInRole("Member") && isActiveMember)
                return View(club);

            return Forbid(); // Người không liên quan không được xem
        }

        // GET: Clubs/Join/5
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> Join(int clubId)
        {
            var user = await _userManager.GetUserAsync(User);
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
            if (student == null) return NotFound("Không tìm thấy sinh viên");

            // Lấy membership mới nhất của user với club này (nếu có)
            var existingMembership = await _context.Memberships
                .Where(m => m.StudentId == student.Id && m.ClubId == clubId)
                .OrderByDescending(m => m.Id)
                .FirstOrDefaultAsync();

            if (existingMembership != null)
            {
                // Nếu đã là thành viên active, không cho apply lại
                if (existingMembership.Status == MembershipStatus.Approved && existingMembership.IsActive)
                {
                    TempData["Info"] = "Bạn đã là thành viên của CLB này.";
                    return RedirectToAction("Details", new { id = clubId });
                }
                // Nếu đã từng apply, chỉ cập nhật trạng thái về Pending
                existingMembership.Status = MembershipStatus.Pending;
                existingMembership.IsActive = false;
                existingMembership.JoinDate = DateTime.Now;
                await _context.SaveChangesAsync();

                TempData["Success"] = "Yêu cầu tham gia đã được gửi!";
                return RedirectToAction("Details", new { id = clubId });
            }
            else
            {
                // Chưa từng xin gia nhập, tạo mới
                var membership = new Membership
                {
                    ClubId = clubId,
                    StudentId = student.Id,
                    ApplicationUserId = user.Id,
                    JoinDate = DateTime.Now,
                    Status = MembershipStatus.Pending,
                    IsActive = false
                };

                _context.Memberships.Add(membership);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Yêu cầu tham gia đã được gửi!";
                return RedirectToAction("Details", new { id = clubId });
            }
        }

        [Authorize(Roles = "Admin,ClubManager")]
        public async Task<IActionResult> MembershipRequests(int clubId)
        {
            var requests = await _context.Memberships
                .Include(m => m.Student)
                .Where(m => m.ClubId == clubId && m.Status == MembershipStatus.Pending && m.IsActive == false)
                .ToListAsync();

            ViewBag.ClubId = clubId;
            return View(requests);
        }

        [Authorize(Roles = "Admin,ClubManager")]
        public async Task<IActionResult> ApproveRequest(int id)
        {
            var request = await _context.Memberships.FindAsync(id);
            if (request == null) return NotFound();

            request.Status = MembershipStatus.Approved;
            request.IsActive = true;
            await _context.SaveChangesAsync();

            return RedirectToAction("MembershipRequests", new { clubId = request.ClubId });
        }

        [Authorize(Roles = "Admin,ClubManager")]
        public async Task<IActionResult> RejectRequest(int id)
        {
            var request = await _context.Memberships.FindAsync(id);
            if (request == null) return NotFound();

            request.Status = MembershipStatus.Rejected;
            request.IsActive = false;
            await _context.SaveChangesAsync();

            return RedirectToAction("MembershipRequests", new { clubId = request.ClubId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ClubManager")]
        public async Task<IActionResult> RemoveMember(int id)
        {
            var membership = await _context.Memberships.FindAsync(id);
            if (membership == null) return NotFound();

            // Đánh dấu không hoạt động thay vì xoá
            membership.IsActive = false;
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = membership.ClubId });
        }
    }
}
