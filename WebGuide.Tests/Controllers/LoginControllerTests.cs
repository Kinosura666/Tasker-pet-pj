using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using WebGuide.Controllers;
using WebGuide.Data;
using WebGuide.Models;
using Xunit;

namespace WebGuide.Tests.Controllers
{
    public class LoginControllerTests
    {
        [Fact]
        public void Index_Get_ReturnsViewResult()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Login_Index_Get")
                .Options;

            using var context = new ApplicationDbContext(options);
            var controller = new LoginController(context);

            var result = controller.Index();

            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Index_Post_UserNotFound_ReturnsViewWithModelError()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Login_UserNotFound")
                .Options;

            using var context = new ApplicationDbContext(options);
            var controller = new LoginController(context);

            var loginModel = new Login
            {
                Email = "notfound@example.com",
                Password = "any"
            };

            var result = await controller.Index(loginModel) as ViewResult;

            Assert.NotNull(result);
            Assert.IsType<Login>(result.Model);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey(""));
        }

        [Fact]
        public async Task Index_Post_InvalidPassword_ReturnsViewWithModelError()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Login_InvalidPassword")
                .Options;

            using (var context = new ApplicationDbContext(options))
            {
                var hashed = Convert.ToBase64String(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("correct_password")));

                context.Users.Add(new User
                {
                    Email = "user@example.com",
                    Username = "User",
                    PasswordHash = hashed
                });

                await context.SaveChangesAsync();
            }

            using (var context = new ApplicationDbContext(options))
            {
                var controller = new LoginController(context);

                var model = new Login
                {
                    Email = "user@example.com",
                    Password = "wrong_password"
                };

                var result = await controller.Index(model) as ViewResult;

                Assert.NotNull(result);
                Assert.IsType<Login>(result.Model);
                Assert.False(controller.ModelState.IsValid);
                Assert.True(controller.ModelState.ContainsKey(""));
            }
        }

        [Fact]
        public async Task Index_Post_ValidUser_RedirectsToProfile()
        {
            var urlHelperFactory = new Mock<IUrlHelperFactory>();
            var urlHelper = new Mock<IUrlHelper>();
            urlHelper.Setup(u => u.Action(It.IsAny<UrlActionContext>())).Returns("mocked-url");
            urlHelperFactory.Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>())).Returns(urlHelper.Object);
            
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("Login_ValidUser")
                .Options;

            var email = "user@example.com";
            var password = "correct_password";
            var hashed = Convert.ToBase64String(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(password)));

            using (var context = new ApplicationDbContext(options))
            {
                context.Users.Add(new User
                {
                    Email = email,
                    Username = "User",
                    PasswordHash = hashed
                });

                await context.SaveChangesAsync();
            }

            using (var context = new ApplicationDbContext(options))
            {
                var controller = new LoginController(context);

                var authServiceMock = new Mock<IAuthenticationService>();
                var serviceProviderMock = new Mock<IServiceProvider>();
                serviceProviderMock
                     .Setup(sp => sp.GetService(typeof(IAuthenticationService)))
                     .Returns(authServiceMock.Object);
                serviceProviderMock
                     .Setup(sp => sp.GetService(typeof(IUrlHelperFactory)))
                     .Returns(urlHelperFactory.Object);

                var httpContext = new DefaultHttpContext
                {
                    RequestServices = serviceProviderMock.Object
                };

                controller.ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                };

                var model = new Login
                {
                    Email = email,
                    Password = password
                };

                var result = await controller.Index(model);
                var redirectResult = Assert.IsType<RedirectToActionResult>(result);
                Assert.Equal("Index", redirectResult.ActionName);
                Assert.Equal("Profile", redirectResult.ControllerName);
            }
        }

        [Fact]
        public async Task Logout_SignsOutUserAndRedirectsToHome()
        {
            var contextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("LogoutTestDB")
                .Options;

            using var context = new ApplicationDbContext(contextOptions);
            var controller = new LoginController(context);

            var authMock = new Mock<IAuthenticationService>();
            var urlHelperFactoryMock = new Mock<IUrlHelperFactory>();

            var services = new ServiceCollection();
            services.AddSingleton(authMock.Object);
            services.AddSingleton(urlHelperFactoryMock.Object);
            var serviceProvider = services.BuildServiceProvider();

            var httpContext = new DefaultHttpContext
            {
                RequestServices = serviceProvider,
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Email, "test@example.com")
                }, "mock"))
            };

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            var result = await controller.Logout();

            authMock.Verify(s => s.SignOutAsync(
                httpContext,
                CookieAuthenticationDefaults.AuthenticationScheme,
                It.IsAny<AuthenticationProperties>()), Times.Once);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("Home", redirect.ControllerName);
        }


    }
}
