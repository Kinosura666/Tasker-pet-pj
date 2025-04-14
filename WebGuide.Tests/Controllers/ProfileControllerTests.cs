using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebGuide.Controllers;
using WebGuide.Data;
using WebGuide.Models;
using WebGuide.Services;
using Xunit;

namespace WebGuide.Tests.Controllers
{
    public class ProfileControllerTests
    {
        [Fact]
        public async Task Index_ReturnsViewWithProfileModel()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("ProfileController_Index")
                .Options;

            var testUser = new User
            {
                Id = 1,
                Email = "test@example.com",
                Username = "TestUser",
                PasswordHash = "hashed",
                CreatedAt = DateTime.UtcNow,
                ProfileImageUrl = "http://image.com/avatar.jpg"
            };

            using (var context = new ApplicationDbContext(options))
            {
                context.Users.Add(testUser);
                context.Tasks.Add(new TaskEntity
                {
                    Title = "Test",
                    Deadline = DateTime.Now.AddDays(1),
                    User = testUser,
                    Description = "D",
                    Priority = 1
                });
                await context.SaveChangesAsync();
            }

            var mockClient = new Mock<StorageClient>();
            var cloudStorageService = new GoogleCloudStorageService(mockClient.Object, "test-bucket");

            using (var context = new ApplicationDbContext(options))
            {
                var controller = new ProfileController(context, cloudStorageService);

                var claims = new List<Claim> { new Claim(ClaimTypes.Email, testUser.Email) };
                var identity = new ClaimsIdentity(claims, "Test");
                var userPrincipal = new ClaimsPrincipal(identity);

                var httpContext = new DefaultHttpContext();
                httpContext.User = userPrincipal;

                var sessionMock = new Mock<ISession>();

                var tokenBytes = System.Text.Encoding.UTF8.GetBytes("some_token");

                sessionMock
                    .Setup(s => s.TryGetValue("GoogleAccessToken", out It.Ref<byte[]>.IsAny))
                    .Returns((string key, out byte[] value) =>
                    {
                        value = tokenBytes;
                        return true;
                    });

                httpContext.Session = sessionMock.Object;

                controller.ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                };

                var result = await controller.Index() as ViewResult;
                var model = result?.Model as ProfileModel;

                Assert.NotNull(model);
                Assert.Equal("TestUser", model.Username);
                Assert.Equal("test@example.com", model.Email);
                Assert.Equal(1, model.TasksCount);
                Assert.Equal("http://image.com/avatar.jpg", model.ExistingImageUrl);
            }
        }

        [Fact]
        public async Task Edit_Get_ReturnsViewWithCorrectModel()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("ProfileController_EditGet")
                .Options;

            var testUser = new User
            {
                Id = 1,
                Email = "test@example.com",
                Username = "TestUser",
                PasswordHash = "hashed",
                CreatedAt = DateTime.UtcNow,
                ProfileImageUrl = "http://image.com/avatar.jpg"
            };

            using (var context = new ApplicationDbContext(options))
            {
                context.Users.Add(testUser);
                await context.SaveChangesAsync();
            }

            var mockClient = new Mock<StorageClient>();
            var cloudStorageService = new GoogleCloudStorageService(mockClient.Object, "test-bucket");

            using (var context = new ApplicationDbContext(options))
            {
                var controller = new ProfileController(context, cloudStorageService);

                var claims = new List<Claim> { new Claim(ClaimTypes.Email, testUser.Email) };
                var identity = new ClaimsIdentity(claims, "Test");
                var userPrincipal = new ClaimsPrincipal(identity);

                var httpContext = new DefaultHttpContext { User = userPrincipal };
                controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

                var result = await controller.Edit() as ViewResult;
                var model = result?.Model as ProfileModel;

                Assert.NotNull(model);
                Assert.Equal("TestUser", model.Username);
                Assert.Equal("test@example.com", model.Email);
                Assert.Equal("http://image.com/avatar.jpg", model.ExistingImageUrl);
            }
        }

        [Fact]
        public async Task DeleteAccount_RemovesUserAndRedirects()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("DeleteAccountTest")
                .Options;

            var testUser = new User
            {
                Id = 1,
                Email = "test@example.com",
                Username = "TestUser",
                PasswordHash = "hashed"
            };

            using (var context = new ApplicationDbContext(options))
            {
                context.Users.Add(testUser);
                await context.SaveChangesAsync();
            }

            using (var context = new ApplicationDbContext(options))
            {
                var storageClient = new Mock<StorageClient>();
                var service = new GoogleCloudStorageService(storageClient.Object, "fake-bucket");

                var controller = new ProfileController(context, service);

                var claims = new List<Claim> { new Claim(ClaimTypes.Email, testUser.Email) };
                var identity = new ClaimsIdentity(claims, "TestAuth");
                var userPrincipal = new ClaimsPrincipal(identity);

                var httpContext = new DefaultHttpContext
                {
                    User = userPrincipal
                };

                var authMock = new Mock<IAuthenticationService>();
                authMock.Setup(a => a.SignOutAsync(
                    httpContext,
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    It.IsAny<AuthenticationProperties>()
                )).Returns(Task.CompletedTask);

                var services = new ServiceCollection();
                services.AddSingleton<IAuthenticationService>(authMock.Object);

                httpContext.RequestServices = services.BuildServiceProvider();

                var mockUrlHelper = new Mock<IUrlHelper>();
                mockUrlHelper
                    .Setup(x => x.Action(It.IsAny<UrlActionContext>()))
                    .Returns("/");

                controller.Url = mockUrlHelper.Object;

                controller.ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                };

                var result = await controller.DeleteAccount();

                var redirect = Assert.IsType<RedirectToActionResult>(result);
                Assert.Equal("Index", redirect.ActionName);
                Assert.Equal("Home", redirect.ControllerName);
                Assert.False(await context.Users.AnyAsync(u => u.Email == testUser.Email));
            }
        }

    }
}