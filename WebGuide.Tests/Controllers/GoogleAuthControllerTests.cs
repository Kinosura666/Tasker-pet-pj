using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebGuide.Controllers;
using WebGuide.Data;
using WebGuide.Models;
using WebGuide.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace WebGuide.Tests.Controllers
{
    public class GoogleAuthControllerTests
    {
        [Fact]
        public void LoginWithGoogle_ReturnsRedirectResultWithGoogleUrl()
        {
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["Authentication:Google:ClientId"]).Returns("fake-client-id");

            var controller = new GoogleAuthController(mockConfig.Object, null);

            var result = controller.LoginWithGoogle() as RedirectResult;

            Assert.NotNull(result);
            Assert.StartsWith("https://accounts.google.com/o/oauth2/v2/auth", result.Url);
            Assert.Contains("client_id=fake-client-id", result.Url);
        }
    }
}

