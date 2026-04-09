using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace ExcOrganizer.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public ProfileController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            return View(user);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeEmail(string newEmail)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (string.IsNullOrWhiteSpace(newEmail))
            {
                TempData["Error"] = "Имейлът не може да е празен.";
                return RedirectToAction("Index");
            }

            var existingUser = await _userManager.FindByEmailAsync(newEmail);
            if (existingUser != null && existingUser.Id != user.Id)
            {
                TempData["Error"] = "Този имейл вече се използва.";
                return RedirectToAction("Index");
            }

            user.Email = newEmail;
            
            user.NormalizedEmail = newEmail.ToUpper();
            

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["Success"] = "Имейлът беше успешно променен.";
            }
            else
            {
                TempData["Error"] = "Грешка при промяна на имейла.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                TempData["Error"] = "Паролата не може да е празна.";
                return RedirectToAction("Index");
            }

            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "Новата парола и потвърждението не съвпадат.";
                return RedirectToAction("Index");
            }

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["Success"] = "Паролата беше успешно променена.";
            }
            else
            {
                TempData["Error"] = "Грешна текуща парола. Опитай отново.";
            }

            return RedirectToAction("Index");
        }

    }
}