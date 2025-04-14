using Xunit;
using Moq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using WebGuide.Models;
using WebGuide.Services;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Moq.Protected;
using System;

namespace WebGuide.Tests.Services
{
    public class TrelloServiceTests
    {
        [Fact]
        public async Task CreateCardAsync_ShouldReturnTrue_WhenRequestIsSuccessful()
        {
            var task = new TaskEntity
            {
                Title = "Test Task",
                Description = "Test Description",
                Deadline = DateTime.UtcNow.AddDays(1)
            };

            var handlerMock = new Mock<HttpMessageHandler>();

            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync",
                   ItExpr.IsAny<HttpRequestMessage>(),
                   ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(new HttpResponseMessage
               {
                   StatusCode = HttpStatusCode.OK,
               });

            var httpClient = new HttpClient(handlerMock.Object);

            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["Trello:ApiKey"]).Returns("fake_key");
            configMock.Setup(c => c["Trello:Token"]).Returns("fake_token");
            configMock.Setup(c => c["Trello:ListId"]).Returns("list_id");

            var service = new TrelloService(httpClient, configMock.Object);

            var result = await service.CreateCardAsync(task);

            Assert.True(result);
        }

        [Fact]
        public async Task CreateCardAsync_ShouldReturnFalse_WhenRequestFails()
        {
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("Invalid request")
                });

            var httpClient = new HttpClient(handlerMock.Object);

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "Trello:ApiKey", "test-key" },
                    { "Trello:Token", "test-token" },
                    { "Trello:ListId", "list-123" }
                })
                .Build();

            var service = new TrelloService(httpClient, configuration);

            var task = new TaskEntity
            {
                Title = "Invalid Task",
                Description = "Missing required fields",
                Deadline = DateTime.UtcNow.AddDays(1)
            };

            var result = await service.CreateCardAsync(task);

            Assert.False(result);
        }

        [Fact]
        public async Task CreateCardAsync_SendsCorrectRequestData()
        {
            HttpRequestMessage sentRequest = null;

            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((req, _) => sentRequest = req)
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{}")
                });

            var httpClient = new HttpClient(handlerMock.Object);

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
            { "Trello:ApiKey", "key" },
            { "Trello:Token", "token" },
            { "Trello:ListId", "list123" }
                })
                .Build();

            var service = new TrelloService(httpClient, configuration);

            var task = new TaskEntity
            {
                Title = "Test",
                Description = "Test Desc",
                Deadline = DateTime.UtcNow
            };

            await service.CreateCardAsync(task);

            Assert.NotNull(sentRequest);
            Assert.Equal(HttpMethod.Post, sentRequest.Method);
            Assert.Contains("https://api.trello.com/1/cards?key=key&token=token", sentRequest.RequestUri.ToString());

            var body = await sentRequest.Content.ReadAsStringAsync();
            Assert.Contains("idList=list123", body);
            Assert.Contains("name=Test", body);
            Assert.Contains("desc=Test+Desc", body); 
        }


    }
}
