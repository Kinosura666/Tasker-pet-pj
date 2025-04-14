using Xunit;
using System;
using System.Collections.Generic;
using WebGuide.Models;
using WebGuide.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;
using WebGuide.Controllers;
using WebGuide.Data;
using System.Net.Http;
using System.Net;

namespace WebGuide.Tests.Controllers
{
    public class StatisticsControllerTests
    {
        [Fact]
        public async Task Index_ReturnsViewWithCorrectStatistics()
        {
            var now = DateTime.Now;

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            var user = new User
            {
                Id = 1,
                Email = "user@example.com",
                Username = "TestUser",
                PasswordHash = "hashed"
            };

            var tasks = new List<TaskEntity>
            {
                new TaskEntity { Title = "T1", Deadline = now.AddDays(-1), IsCompleted = false, User = user, Description = "d" },
                new TaskEntity { Title = "T2", Deadline = now.AddDays(1), IsCompleted = true, User = user, Description = "d" },
                new TaskEntity { Title = "T3", Deadline = now.AddDays(5), IsCompleted = false, User = user, Description = "d" }
            };

            context.Users.Add(user);
            context.Tasks.AddRange(tasks);
            await context.SaveChangesAsync();

            var statisticsService = new StatisticsService();
            var controller = new StatisticsController(context, statisticsService);

            var claims = new List<Claim> { new Claim(ClaimTypes.Email, user.Email) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            var result = await controller.Index() as ViewResult;

            Assert.NotNull(result);
            var model = result.Model as Statistics;
            Assert.NotNull(model);
            Assert.Equal(3, model.TotalTasks);
            Assert.Equal(1, model.CompletedTasks);
            Assert.Equal(1, model.OverdueTasks);
        }

    }
}
