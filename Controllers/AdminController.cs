using ClubManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClubManager.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: /Admin/Users
        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users.ToListAsync();
            return View(users);
        }

        // GET: /Admin/EditRole/userId
        public async Task<IActionResult> EditRole(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var allRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            var currentRoles = await _userManager.GetRolesAsync(user);

            ViewBag.AllRoles = allRoles;
            ViewBag.UserId = user.Id;
            ViewBag.CurrentRoles = currentRoles;

            return View(user);
        }

        // POST: /Admin/EditRole
        [HttpPost]
        public async Task<IActionResult> EditRole(string userId, List<string> selectedRoles)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);

            if (!removeResult.Succeeded)
            {
                ModelState.AddModelError("", "Không thể gỡ vai trò cũ.");
                return RedirectToAction("EditRole", new { userId });
            }

            if (selectedRoles != null && selectedRoles.Any())
            {
                var addResult = await _userManager.AddToRolesAsync(user, selectedRoles);
                if (!addResult.Succeeded)
                {
                    ModelState.AddModelError("", "Không thể thêm vai trò mới.");
                    return RedirectToAction("EditRole", new { userId });
                }
            }

            TempData["SuccessMessage"] = "Cập nhật vai trò thành công.";
            return RedirectToAction("Users");
        }

        // POST: /Admin/DeleteUser
        [HttpPost]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Người dùng đã bị xóa.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể xóa người dùng.";
            }

            return RedirectToAction("Users");
        }
    }
}
