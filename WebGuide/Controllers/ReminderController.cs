using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebGuide.Data;
using WebGuide.Models;
using WebGuide.Services;

namespace WebGuide.Controllers
{
    public class ReminderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly MailjetEmail _emailService;
        private readonly ILogger<ReminderController> _logger;

        public ReminderController(ApplicationDbContext context, MailjetEmail emailService, ILogger<ReminderController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> SendNow()
        {
            var now = DateTime.UtcNow;

            var tasks = await _context.Tasks
                .Include(t => t.User)
                .Where(t => t.Deadline > now)
                .ToListAsync();

            var tasksToRemind = new List<TaskEntity>();

            foreach (var task in tasks)
            {
                var hoursLeft = (task.Deadline - now).TotalHours;

                if (!task.Reminder24hSent && Math.Abs(hoursLeft - 24) < 0.4)
                {
                    task.Reminder24hSent = true;
                    tasksToRemind.Add(task);
                }
                else if (!task.Reminder12hSent && Math.Abs(hoursLeft - 12) < 0.4)
                {
                    task.Reminder12hSent = true;
                    tasksToRemind.Add(task);
                }
                else if (!task.Reminder2hSent && Math.Abs(hoursLeft - 2) < 0.4)
                {
                    task.Reminder2hSent = true;
                    tasksToRemind.Add(task);
                }

                //Console.WriteLine($"Завдання: {task.Title} | дедлайн: {task.Deadline:f} | до дедлайну: {hoursLeft} год");
            }

            foreach (var task in tasksToRemind)
            {
                var timeLeft = task.Deadline - now;

                var html = $@"
                <h3>⏰ Нагадування про дедлайн завдання</h3>
                <p><strong>Завдання:</strong> {task.Title}</p>
                <p><strong>Залишилось:</strong> {Math.Round(timeLeft.TotalHours)} год.</p>
                <p><strong>Опис:</strong> {task.Description}</p>";

                var success = await _emailService.SendEmailAsync(
                    toEmail: task.User.Email,
                    toName: task.User.Username,
                    subject: "Нагадування про дедлайн завдання",
                    htmlContent: html
                );

                if (success)
                {
                    _logger.LogInformation($"Email надіслано вручну для завдання ID {task.Id}");
                    task.LastReminderSentAt = now;
                }
                else
                {
                    _logger.LogWarning($"Помилка під час надсилання email для завдання ID {task.Id}");
                }
            }

            await _context.SaveChangesAsync();

            return Content($"Готово. Надіслано {tasksToRemind.Count} нагадування.");
        }
    }
}
