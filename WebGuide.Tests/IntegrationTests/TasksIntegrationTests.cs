using System.Linq;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using WebGuide.Data;
using WebGuide.Models;
using WebGuide.Tests.Factories;
using Xunit;
using System.Collections.Generic;

namespace WebGuide.Tests.IntegrationTests
{
    public class TasksIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly CustomWebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly ApplicationDbContext _dbContext;


        public TasksIntegrationTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            var scope = factory.Services.CreateScope();
            _dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (!_dbContext.Users.Any(u => u.Email == "test@example.com"))
            {
                _dbContext.Users.Add(new User
                {
                    Username = "testuser",
                    Email = "test@example.com",
                    PasswordHash = "fake"
                });
                _dbContext.SaveChanges();
            }
        }


        [Fact]
        public async Task CreateTask_Should_SaveTaskInDatabase()
        {
            var formData = new MultipartFormDataContent
            {
                { new StringContent("Test task title"), "Title" },
                { new StringContent("Integration test description"), "Description" },
                { new StringContent(DateTime.Now.AddDays(1).ToString("o")), "Deadline" },
                { new StringContent("1"), "Priority" },
                { new StringContent("false"), "AddToGoogleCalendar" }
            };

            var response = await _client.PostAsync("/Create", formData);

            response.StatusCode.Should().Be(HttpStatusCode.Redirect);

            var createdTask = _dbContext.Tasks.FirstOrDefault(t => t.Title == "Test task title");
            createdTask.Should().NotBeNull();
            createdTask.Description.Should().Be("Integration test description");
        }

        [Fact]
        public async Task Tasks_Complete_Should_SetIsCompletedToTrue()
        {
            var user = _dbContext.Users.First(u => u.Email == "test@example.com");

            var task = new TaskEntity
            {
                Title = "Complete me",
                Description = "To be completed",
                Deadline = DateTime.Now.AddDays(1),
                Priority = 1,
                UserId = user.Id,
                IsCompleted = false
            };
            _dbContext.Tasks.Add(task);
            _dbContext.SaveChanges();

            var response = await _client.PostAsync($"/Tasks/Complete/{task.Id}", new FormUrlEncodedContent([]));

            response.StatusCode.Should().Be(HttpStatusCode.Redirect);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var updated = db.Tasks.First(t => t.Id == task.Id);

            updated.IsCompleted.Should().BeTrue();
            updated.CompletedAt.Should().NotBeNull();
        }


    }
}
