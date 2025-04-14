using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace WebGuide.Services
{
    public class MailjetEmail
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public MailjetEmail(IConfiguration configuration)
        {
            _configuration = configuration;
            _httpClient = new HttpClient();

            var apiKey = _configuration["Mailjet:ApiKey"];
            var secretKey = _configuration["Mailjet:SecretKey"];

            var byteArray = Encoding.ASCII.GetBytes($"{apiKey}:{secretKey}");
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
        }

        public MailjetEmail(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;

            var apiKey = _configuration["Mailjet:ApiKey"];
            var secretKey = _configuration["Mailjet:SecretKey"];

            var byteArray = Encoding.ASCII.GetBytes($"{apiKey}:{secretKey}");
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
        }

        public async Task<bool> SendEmailAsync(string toEmail, string toName, string subject, string htmlContent)
        {
            var senderEmail = _configuration["Mailjet:SenderEmail"];
            var senderName = _configuration["Mailjet:SenderName"];

            var body = new
            {
                Messages = new[]
                {
            new
            {
                From = new { Email = senderEmail, Name = senderName },
                To = new[] { new { Email = toEmail, Name = toName } },
                Subject = subject,
                HTMLPart = htmlContent
            }
        }
            };

            var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://api.mailjet.com/v3.1/send", content);

            // 💡 Додай логування статусу + повідомлення
            var responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"📬 MAILJET STATUS: {(int)response.StatusCode} {response.ReasonPhrase}");
            Console.WriteLine($"📨 MAILJET RESPONSE BODY: {responseBody}");

            return response.IsSuccessStatusCode;
        }

    }
}
