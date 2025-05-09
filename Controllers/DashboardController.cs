using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ClubManager.Models;
using Microsoft.AspNetCore.Authorization;

namespace ClubManager.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // Lấy thông tin người dùng đã đăng nhập
            var user = await _userManager.GetUserAsync(User);
            return View(user); // Trả về view với thông tin người dùng
        }
    }
}
