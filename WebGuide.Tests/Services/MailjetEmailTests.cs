using Xunit;
using Moq;
using Microsoft.Extensions.Configuration;
using WebGuide.Services;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq.Protected;
using System.Collections.Generic;

namespace WebGuide.Tests.Services
{
    public class MailjetEmailTests
    {
        [Fact]
        public async Task SendEmailAsync_ReturnsTrue_WhenSuccessful()
        {
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>()
               )
               .ReturnsAsync(new HttpResponseMessage
               {
                   StatusCode = HttpStatusCode.OK,
                   Content = new StringContent("{ \"message\": \"success\" }"),
               });

            var httpClient = new HttpClient(handlerMock.Object);

            var inMemorySettings = new Dictionary<string, string> {
                {"Mailjet:ApiKey", "fake-key"},
                {"Mailjet:SecretKey", "fake-secret"},
                {"Mailjet:SenderEmail", "sender@example.com"},
                {"Mailjet:SenderName", "WebGuide"}
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var service = new MailjetEmail(configuration, httpClient);

            var result = await service.SendEmailAsync("to@example.com", "User", "Test subject", "<p>Hello</p>");

            Assert.True(result);

            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri.ToString() == "https://api.mailjet.com/v3.1/send"
                ),
                ItExpr.IsAny<CancellationToken>()
            );
        }
    }
}
