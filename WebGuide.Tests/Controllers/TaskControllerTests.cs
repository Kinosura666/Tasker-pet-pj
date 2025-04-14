using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using WebGuide.Controllers;
using WebGuide.Models;
using WebGuide.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System;
using System.Net.Http;
using WebGuide.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace WebGuide.Tests.Controllers
{
    public class TasksControllerTests
    {
        [Fact]
        public async Task Complete_ShouldMarkTaskAsCompleted_AndRedirect()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TasksTestDB")
                .Options;

            var task = new TaskEntity
            {
                Id = 1,
                Title = "Test Task",
                Description = "Sample description", 
                IsCompleted = false
            };


            using (var context = new ApplicationDbContext(options))
            {
                context.Tasks.Add(task);
                await context.SaveChangesAsync();
            }

            var loggerMock = new Mock<ILogger<TasksController>>();
            var trelloMock = new Mock<TrelloService>(MockBehavior.Loose, new HttpClient(), Mock.Of<IConfiguration>());

            using (var context = new ApplicationDbContext(options))
            {
                var controller = new TasksController(context, trelloMock.Object, loggerMock.Object);

                var result = await controller.Complete(1);

                var updatedTask = context.Tasks.First();
                Assert.True(updatedTask.IsCompleted);
                Assert.IsType<RedirectToActionResult>(result);
                var redirect = result as RedirectToActionResult;
                Assert.Equal("Index", redirect.ActionName);
            }
        }

        [Fact]
        public async Task Index_ShouldReturnTasks_FilteredAndSorted()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("TasksTestDB_Index")
                .Options;

            using (var context = new ApplicationDbContext(options))
            {
                var user = new User
                {
                    Id = 1,
                    Email = "user@example.com",
                    Username = "TestUser",
                    PasswordHash = "hashed"
                };
                var otherUser = new User
                {
                    Id = 2,
                    Email = "other@example.com",
                    Username = "OtherUser",
                    PasswordHash = "otherhashed"
                };

                context.Users.AddRange(user, otherUser);
                context.Tasks.AddRange(
                    new TaskEntity { Title = "T1", Priority = 1, User = user, Description = "desc 1", Deadline = DateTime.Now.AddHours(2), IsCompleted = false },
                    new TaskEntity { Title = "T2", Priority = 2, User = user, Description = "desc 2", Deadline = DateTime.Now.AddHours(4), IsCompleted = false },
                    new TaskEntity { Title = "T3", Priority = 3, User = otherUser, Description = "desc 3", Deadline = DateTime.Now.AddHours(6), IsCompleted = false }
                );
                await context.SaveChangesAsync();
            }

            var loggerMock = new Mock<ILogger<TasksController>>();
            var trelloMock = new Mock<TrelloService>(MockBehavior.Loose, new HttpClient(), Mock.Of<IConfiguration>());
            using (var context = new ApplicationDbContext(options))
            {
                var controller = new TasksController(context, trelloMock.Object, loggerMock.Object);

                var user = new User
                {
                    Id = 1,
                    Email = "user@example.com",
                    Username = "TestUser",
                    PasswordHash = "hashed"
                };


                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Email, user.Email)
                };
                var identity = new ClaimsIdentity(claims, "TestAuth");
                var userPrincipal = new ClaimsPrincipal(identity);

                controller.ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = userPrincipal }
                };

                var result = await controller.Index("priorityAsc", 1, "false") as ViewResult;
                var model = result?.Model as List<TaskEntity>;

                Assert.NotNull(model);
                Assert.Single(model); 
                Assert.Equal("T1", model.First().Title);
            }
        }

        [Fact]
        public async Task Details_ReturnsNotFound_WhenIdIsNull()
        {
            var context = GetFakeContext(); 
            var controller = GetControllerWithContext(context);

            var result = await controller.Details(null);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Details_ReturnsNotFound_WhenTaskNotFound()
        {
            var context = GetFakeContext();
            var controller = GetControllerWithContext(context);

            var result = await controller.Details(999); 

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Details_ReturnsView_WhenTaskFound()
        {
            var context = GetFakeContext();
            var user = new User { Id = 1, Email = "test@mail.com", Username = "User", PasswordHash = "pass" };
            var task = new TaskEntity
            {
                Id = 1,
                Title = "My Task",
                Description = "Test",
                User = user,
                Deadline = DateTime.Now
            };
            context.Users.Add(user);
            context.Tasks.Add(task);
            await context.SaveChangesAsync();

            var controller = GetControllerWithContext(context);

            var result = await controller.Details(1) as ViewResult;

            Assert.NotNull(result);
            var model = result.Model as TaskEntity;
            Assert.Equal("My Task", model.Title);
        }

        private ApplicationDbContext GetFakeContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private TasksController GetControllerWithContext(ApplicationDbContext context)
        {
            var logger = new Mock<ILogger<TasksController>>();
            var trello = new Mock<TrelloService>(MockBehavior.Loose, new HttpClient(), Mock.Of<IConfiguration>());
            return new TasksController(context, trello.Object, logger.Object);
        }

        [Fact]
        public async Task Create_AddsTaskToDatabase_WhenModelIsValid()
        {
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
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var logger = new Mock<ILogger<TasksController>>();
            var trelloMock = new Mock<TrelloService>(new HttpClient(), Mock.Of<IConfiguration>());
            var controller = new TasksController(context, trelloMock.Object, logger.Object);

            var claims = new List<Claim> { new Claim(ClaimTypes.Email, user.Email) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            var model = new TaskEntity
            {
                Title = "Test Task",
                Description = "Some description",
                Deadline = DateTime.Now.AddHours(1),
                Priority = 2
            };

            var result = await controller.Create(model, null) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);

            var taskInDb = context.Tasks.FirstOrDefault();
            Assert.NotNull(taskInDb);
            Assert.Equal("Test Task", taskInDb.Title);
            Assert.Equal(user.Id, taskInDb.UserId);
        }

    }
}