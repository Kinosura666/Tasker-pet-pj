using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using WebGuide.Models;

namespace WebGuide.Services
{
    public class GoogleCalendarService
    {
        private CalendarService? calendarService;

        public async Task AddTaskToCalendarAsync(TaskEntity task, string accessToken)
        {
            if (calendarService == null)
            {
                var credential = GoogleCredential.FromAccessToken(accessToken);
                calendarService = new CalendarService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "WebGuide Task Calendar"
                });
            }

            var newEvent = new Event
            {
                Summary = task.Title,
                Description = task.Description,
                Start = new EventDateTime
                {
                    DateTime = task.Deadline,
                    TimeZone = "Europe/Kyiv"
                },
                End = new EventDateTime
                {
                    DateTime = task.Deadline.AddMinutes(30),
                    TimeZone = "Europe/Kyiv"
                }
            };

            await calendarService.Events.Insert(newEvent, "primary").ExecuteAsync();
        }

        public void OverrideCalendarService(CalendarService customService)
        {
            calendarService = customService;
        }
    }

}
