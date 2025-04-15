using System.Net.Http;
using System.Net.Http.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using WebGuide.Models;

namespace WebGuide.Services
{
    public class TrelloService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _token;
        private readonly string _listId;

        public TrelloService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Trello:ApiKey"];
            _token = configuration["Trello:Token"];
            _listId = configuration["Trello:ListId"];
        }

        public async Task<bool> CreateCardAsync(TaskEntity task)
        {
            var url = $"https://api.trello.com/1/cards?key={_apiKey}&token={_token}";
            var values = new Dictionary<string, string>
            {
                { "idList", _listId },
                { "name", task.Title },
                { "desc", task.Description },
                { "due", task.Deadline.ToString("o") }
            };

            var content = new FormUrlEncodedContent(values);
            var response = await _httpClient.PostAsync(url, content);

            return response.IsSuccessStatusCode;
        }
    }
}
