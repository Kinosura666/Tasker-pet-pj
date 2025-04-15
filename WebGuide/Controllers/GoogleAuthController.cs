using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2.Responses;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Claims;
using WebGuide.Data;
using WebGuide.Models;
using Microsoft.EntityFrameworkCore;

namespace WebGuide.Controllers
{
    public class GoogleAuthController : Controller
    {
        private readonly IConfiguration _config;
        private readonly ApplicationDbContext _context;

        public GoogleAuthController(IConfiguration config, ApplicationDbContext context)
        {
            _config = config;
            _context = context;
        }

        [HttpGet("/google-login")]
        public IActionResult LoginWithGoogle()
        {
            var clientId = _config["Authentication:Google:ClientId"];
            var redirectUri = _config["Authentication:Google:RedirectUri"];
            Console.WriteLine($"Google Client ID: {clientId}");
            Console.WriteLine($"Google Redirect URI: {redirectUri}");

            if (string.IsNullOrEmpty(redirectUri))
            {
                return Content("❌ redirect_uri is empty! Перевір конфігурацію.");
            }
            var scopes = string.Join(" ", new[]
            {
                "https://www.googleapis.com/auth/calendar.events",
                "https://www.googleapis.com/auth/userinfo.email",
                "https://www.googleapis.com/auth/userinfo.profile"
            });

            var url = QueryHelpers.AddQueryString("https://accounts.google.com/o/oauth2/v2/auth", new Dictionary<string, string>
            {
                { "client_id", clientId },
                { "redirect_uri", redirectUri },
                { "response_type", "code" },
                { "scope", scopes },
                { "access_type", "offline" },
                { "prompt", "consent" }
            });

            return Redirect(url);
        }


        [HttpGet("/oauth2callback")]
        public async Task<IActionResult> GoogleCallback([FromQuery] string code)
        {
            var clientSecrets = new ClientSecrets
            {
                ClientId = _config["Authentication:Google:ClientId"],
                ClientSecret = _config["Authentication:Google:ClientSecret"]
            };

            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = clientSecrets,
                Scopes = new[] { 
                    "https://www.googleapis.com/auth/calendar.events", 
                    "https://www.googleapis.com/auth/userinfo.email", 
                    "https://www.googleapis.com/auth/userinfo.profile" }
            });

            var token = await flow.ExchangeCodeForTokenAsync(
                userId: "",
                code: code,
                redirectUri: _config["Authentication:Google:RedirectUri"],
                taskCancellationToken: CancellationToken.None
            );

            var credential = new UserCredential(flow, "", token);

            var oauthService = new HttpClient();
            oauthService.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);
            var userInfoJson = await oauthService.GetStringAsync("https://www.googleapis.com/oauth2/v2/userinfo");

            var userInfo = System.Text.Json.JsonDocument.Parse(userInfoJson).RootElement;
            var email = userInfo.GetProperty("email").GetString();
            var name = userInfo.GetProperty("name").GetString();

            //Console.WriteLine($"Google user: {name} ({email})");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                user = new User
                {
                    Email = email,
                    Username = name
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            HttpContext.Session.SetString("GoogleAccessToken", token.AccessToken);

            return RedirectToAction("Index", "Profile");
        }


    }
}
