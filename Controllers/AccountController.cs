using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using ClubManager.Models;
using ClubManager.Data;
using ClubManager.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;

namespace ClubManager.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IWebHostEnvironment _environment;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;

        public AccountController(UserManager<ApplicationUser> userManager,
                                 SignInManager<ApplicationUser> signInManager,
                                 IWebHostEnvironment environment,
                                 RoleManager<IdentityRole> roleManager,
                                 AppDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _environment = environment;
            _roleManager = roleManager;
            _context = context;
        }

        // GET: /Account/Manage
        public async Task<IActionResult> Manage()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.Roles = roles;

            // Lấy danh sách các CLB đã tham gia
            var userClubs = _context.Memberships
                .Where(m => m.ApplicationUserId == user.Id)
                .Select(m => m.Club)
                .ToList();
            ViewBag.UserClubs = userClubs;

            return View(user);
        }


        // POST: /Account/Manage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Manage([Bind("FullName,Class,RoleInClub")] ApplicationUser updatedUser, IFormFile? AvatarFile)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                user.FullName = updatedUser.FullName;
                user.Class = updatedUser.Class;
                user.RoleInClub = updatedUser.RoleInClub;

                // Xử lý upload ảnh đại diện
                if (AvatarFile != null && AvatarFile.Length > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var ext = Path.GetExtension(AvatarFile.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(ext))
                    {
                        ModelState.AddModelError("AvatarFile", "Chỉ cho phép các định dạng ảnh: jpg, jpeg, png, gif.");
                        var roles = await _userManager.GetRolesAsync(user);
                        ViewBag.Roles = roles;
                        return View(user);
                    }
                    if (AvatarFile.Length > 2 * 1024 * 1024)
                    {
                        ModelState.AddModelError("AvatarFile", "Dung lượng ảnh tối đa là 2MB.");
                        var roles = await _userManager.GetRolesAsync(user);
                        ViewBag.Roles = roles;
                        return View(user);
                    }

                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
                    Directory.CreateDirectory(uploadsFolder);

                    // Xóa file cũ nếu có
                    if (!string.IsNullOrEmpty(user.AvatarPath))
                    {
                        var oldAvatarPath = user.AvatarPath.StartsWith("/") ? user.AvatarPath.Substring(1) : user.AvatarPath;
                        var oldAvatarPhysicalPath = Path.Combine(_environment.WebRootPath, oldAvatarPath.Replace("/", Path.DirectorySeparatorChar.ToString()));
                        if (System.IO.File.Exists(oldAvatarPhysicalPath))
                        {
                            try { System.IO.File.Delete(oldAvatarPhysicalPath); } catch { /* ignore */ }
                        }
                    }

                    var fileName = $"{Guid.NewGuid()}{ext}";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    try
                    {
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await AvatarFile.CopyToAsync(stream);
                        }
                        user.AvatarPath = $"/uploads/avatars/{fileName}";
                    }
                    catch
                    {
                        ModelState.AddModelError("AvatarFile", "Có lỗi khi lưu ảnh đại diện.");
                        var roles = await _userManager.GetRolesAsync(user);
                        ViewBag.Roles = roles;
                        return View(user);
                    }
                }

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "Cập nhật thông tin thành công.";
                    return RedirectToAction(nameof(Manage));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            var roles2 = await _userManager.GetRolesAsync(user);
            ViewBag.Roles = roles2;

            return View(user);
        }
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            // Kiểm tra mật khẩu mới không trùng mật khẩu hiện tại
            var passwordHasher = _userManager.PasswordHasher;
            var verifyResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.NewPassword);
            if (verifyResult == PasswordVerificationResult.Success)
            {
                ModelState.AddModelError("NewPassword", "Mật khẩu mới không được trùng với mật khẩu hiện tại.");
                return View(model);
            }

            // Kiểm tra mật khẩu mới không trùng UserName
            if (!string.IsNullOrEmpty(user.UserName) && model.NewPassword == user.UserName)
            {
                ModelState.AddModelError("NewPassword", "Mật khẩu mới không được trùng với tên đăng nhập.");
                return View(model);
            }

            // Kiểm tra mật khẩu mới không trùng FullName
            if (!string.IsNullOrEmpty(user.FullName) && model.NewPassword == user.FullName)
            {
                ModelState.AddModelError("NewPassword", "Mật khẩu mới không được trùng với họ tên.");
                return View(model);
            }

            // Kiểm tra mật khẩu mới không trùng Email
            if (!string.IsNullOrEmpty(user.Email) && model.NewPassword == user.Email)
            {
                ModelState.AddModelError("NewPassword", "Mật khẩu mới không được trùng với email.");
                return View(model);
            }

            // Kiểm tra độ dài tối thiểu
            if (model.NewPassword.Length < 8)
            {
                ModelState.AddModelError("NewPassword", "Mật khẩu mới phải có ít nhất 8 ký tự.");
                return View(model);
            }

            // Kiểm tra có chữ hoa
            if (!model.NewPassword.Any(char.IsUpper))
            {
                ModelState.AddModelError("NewPassword", "Mật khẩu mới phải chứa ít nhất 1 chữ hoa.");
                return View(model);
            }

            // Kiểm tra có chữ thường
            if (!model.NewPassword.Any(char.IsLower))
            {
                ModelState.AddModelError("NewPassword", "Mật khẩu mới phải chứa ít nhất 1 chữ thường.");
                return View(model);
            }

            // Kiểm tra có số
            if (!model.NewPassword.Any(char.IsDigit))
            {
                ModelState.AddModelError("NewPassword", "Mật khẩu mới phải chứa ít nhất 1 số.");
                return View(model);
            }

            // Kiểm tra có ký tự đặc biệt
            if (!model.NewPassword.Any(ch => !char.IsLetterOrDigit(ch)))
            {
                ModelState.AddModelError("NewPassword", "Mật khẩu mới phải chứa ít nhất 1 ký tự đặc biệt.");
                return View(model);
            }

            // Kiểm tra không chứa khoảng trắng
            if (model.NewPassword.Any(char.IsWhiteSpace))
            {
                ModelState.AddModelError("NewPassword", "Mật khẩu mới không được chứa khoảng trắng.");
                return View(model);
            }

            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Đổi mật khẩu thành công.";
                return RedirectToAction("Manage");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAvatar()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            if (!string.IsNullOrEmpty(user.AvatarPath))
            {
                // Đảm bảo đường dẫn vật lý đúng
                var avatarRelativePath = user.AvatarPath.StartsWith("/") ? user.AvatarPath.Substring(1) : user.AvatarPath;
                var avatarPhysicalPath = Path.Combine(_environment.WebRootPath, avatarRelativePath.Replace("/", Path.DirectorySeparatorChar.ToString()));

                if (System.IO.File.Exists(avatarPhysicalPath))
                {
                    try
                    {
                        System.IO.File.Delete(avatarPhysicalPath);
                    }
                    catch
                    {
                        // Nếu xóa file lỗi, vẫn tiếp tục xóa đường dẫn trong DB
                    }
                }

                user.AvatarPath = null;
                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "Đã xóa ảnh đại diện.";
                }
                else
                {
                    TempData["SuccessMessage"] = "Không thể cập nhật thông tin người dùng sau khi xóa ảnh.";
                }
            }
            else
            {
                TempData["SuccessMessage"] = "Không có ảnh đại diện để xóa.";
            }
            return RedirectToAction(nameof(Manage));
        }
    }
}