using ClubManager.Data;
using ClubManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace ClubManager.Controllers
{
    [Authorize]
    public class EventsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EventsController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        // GET: Events
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var events = await _context.Events
                .Include(e => e.Club)
                .Where(e => e.IsActive)
                .ToListAsync();
            return View(events);
        }

        // GET: Events/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var ev = await _context.Events
                .Include(e => e.Club)
                .Include(e => e.Participants)
                    .ThenInclude(p => p.Student)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (ev == null) return NotFound();

            return View(ev);
        }

        // GET: Events/Create
        [Authorize(Roles = "Admin,ClubManager")]
        public IActionResult Create()
        {
            ViewBag.Clubs = _context.Clubs
                .Where(c => c.IsActive)
                .Select(c => new { c.Id, c.Name }).ToList();

            return View();
        }

        // POST: Events/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ClubManager")]
        public async Task<IActionResult> Create(Event @event)
        {
            if (!ModelState.IsValid) return View(@event);

            _context.Add(@event);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Events/Edit/5
        [Authorize(Roles = "Admin,ClubManager")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var @event = await _context.Events.FindAsync(id);
            if (@event == null) return NotFound();

            ViewBag.Clubs = _context.Clubs
                .Where(c => c.IsActive)
                .Select(c => new { c.Id, c.Name }).ToList();

            return View(@event);
        }

        // POST: Events/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ClubManager")]
        public async Task<IActionResult> Edit(int id, Event @event)
        {
            if (id != @event.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(@event);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Events.Any(e => e.Id == @event.Id))
                        return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(@event);
        }

        // GET: Events/Delete/5
        [Authorize(Roles = "Admin,ClubManager")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var @event = await _context.Events
                .Include(e => e.Club)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (@event == null) return NotFound();

            return View(@event);
        }

        // POST: Events/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ClubManager")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var @event = await _context.Events.FindAsync(id);
            if (@event != null)
            {
                _context.Events.Remove(@event);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Events/Join
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Join(int eventId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            // Kiểm tra Student
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            if (student == null)
            {
                TempData["Error"] = "Bạn chưa có hồ sơ sinh viên.";
                return RedirectToAction("Details", new { id = eventId });
            }

            // Lấy sự kiện
            var ev = await _context.Events.Include(e => e.Club).FirstOrDefaultAsync(e => e.Id == eventId);
            if (ev == null) return NotFound();

            // ❗Kiểm tra Membership với CLB của sự kiện
            var inClub = await _context.Memberships.AnyAsync(m =>
                m.StudentId == student.Id &&
                m.ClubId == ev.ClubId &&
                m.Status == MembershipStatus.Approved &&
                m.IsActive);

            if (!inClub)
            {
                TempData["Error"] = "Bạn cần tham gia CLB trước khi tham gia sự kiện.";
                return RedirectToAction("Details", new { id = eventId });
            }

            // Đã tham gia chưa
            var alreadyJoined = await _context.EventParticipants.AnyAsync(p =>
                p.EventId == eventId && p.StudentId == student.Id);

            if (!alreadyJoined)
            {
                _context.EventParticipants.Add(new EventParticipant
                {
                    EventId = eventId,
                    StudentId = student.Id,
                    JoinDate = DateTime.Now,
                    IsActive = true
                });

                await _context.SaveChangesAsync();
                TempData["Success"] = "Bạn đã tham gia sự kiện.";
            }
            else
            {
                TempData["Info"] = "Bạn đã tham gia sự kiện trước đó.";
            }

            return RedirectToAction("Details", new { id = eventId });
        }
    }
}
