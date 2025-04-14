using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebGuide.Models;
using WebGuide.Data;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;

namespace WebGuide.Controllers
{
    public class LoginController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LoginController(ApplicationDbContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Index(Login model)
        {
            if (!ModelState.IsValid) 
            { 
                var errors = ModelState.Values.SelectMany(x => x.Errors);
                foreach (var error in errors)
                {
                    Console.WriteLine("ModelState error:" + error.ErrorMessage);
                }
                return View(model); 
            }

            var normalizedEmail = model.Email.Trim().ToLower();
            var user = _context.Users.SingleOrDefault(u => u.Email.ToLower() == normalizedEmail);
            if (user == null)
            {
                //Console.WriteLine("Користувача не знайдено.");
                ModelState.AddModelError("", "Invalid email or password");
                return View(model);
            }

            var enteredHash = HashPassword(model.Password);
            if (user.PasswordHash != enteredHash)
            {
                //Console.WriteLine("Пароль не збігається.");
                ModelState.AddModelError("", "Invalid email or password");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties { IsPersistent = true };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity), authProperties);
            //Console.WriteLine("Успішний логін");
            return RedirectToAction("Index", "Profile");
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes);
            }
        }
    }
}
