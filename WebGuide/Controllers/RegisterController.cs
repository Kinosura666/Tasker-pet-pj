using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebGuide.Models;
using WebGuide.Data;
using System.Security.Cryptography;

namespace WebGuide.Controllers
{
    public class RegisterController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RegisterController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(Register model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (_context.Users.Any(u => u.Email == model.Email))
                    {
                        ModelState.AddModelError("Email", "Користувач з такою поштою вже існує.");
                        return View("Index", model);
                    }

                    string passwordHash = HashPassword(model.Password);

                    var user = new User
                    {
                        Username = model.Username,
                        Email = model.Email.Trim().ToLower(),
                        PasswordHash = passwordHash,
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.Email, user.Email)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true
                    };

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);
                    return RedirectToAction("Index", "Profile");
                }
                return View("Index", model);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Register exception: " + ex.Message);
                Console.WriteLine("Stack: " + ex.StackTrace);
                throw;
            }
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
