using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebGuide.Data;
using WebGuide.Models;
using WebGuide.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;

namespace WebGuide.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly GoogleCloudStorageService _storage;

        public ProfileController(ApplicationDbContext context, GoogleCloudStorageService storage)
        {
            _context = context;
            _storage = storage;
        }

        public async Task<IActionResult> Index()
        {
            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            var tasksCount = await _context.Tasks.CountAsync(t => t.UserId == user.Id);

            var model = new ProfileModel
            {
                Username = user.Username,
                Email = user.Email,
                CreatedAt = user.Id > 0 ? user.CreatedAt : DateTime.UtcNow,
                TasksCount = tasksCount,
                ExistingImageUrl = user.ProfileImageUrl
            };

            ViewBag.GoogleAuthorized = HttpContext.Session.GetString("GoogleAccessToken") != null;
            
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            var model = new ProfileModel
            {
                Username = user.Username,
                Email = user.Email,
                ExistingImageUrl = user.ProfileImageUrl
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(ProfileModel model)
        {
            if (!ModelState.IsValid)
            {
                // Перевірка помилок:
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                foreach (var error in errors)
                {
                    Console.WriteLine(error); // Або ILogger
                }

                return View(model);
            }

            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            user.Username = model.Username;
            user.Email = model.Email;

            if (model.ProfileImage != null && model.ProfileImage.Length > 0)
            {
                var fileUrl = await _storage.UploadFileAsync(model.ProfileImage.OpenReadStream(), model.ProfileImage.FileName, model.ProfileImage.ContentType);
                user.ProfileImageUrl = fileUrl;
            }

            _context.Update(user);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Index", "Home");
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult ConfirmDelete()
        {
            return View();
        }

    }

}
