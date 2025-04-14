using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebGuide.Data;
using WebGuide.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebGuide.Models;

namespace WebGuide.Services
{
    public class TaskReminderBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TaskReminderBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);

        public TaskReminderBackgroundService(IServiceProvider serviceProvider, ILogger<TaskReminderBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var emailService = scope.ServiceProvider.GetRequiredService<MailjetEmail>();

                    var now = DateTime.UtcNow;

                    var tasks = await context.Tasks
                        .Include(t => t.User)
                        .Where(t => t.Deadline > now)
                        .ToListAsync(stoppingToken);

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

                        //_logger.LogInformation($"Завдання: {task.Title} | до дедлайну: {hoursLeft}");
                    }

                    foreach (var task in tasksToRemind)
                    {
                        var timeLeft = task.Deadline - now;

                        var html = $@"
                        <h3>⏰ Нагадування про дедлайн завдання</h3>
                        <p><strong>Завдання:</strong> {task.Title}</p>
                        <p><strong>Залишилось:</strong> {Math.Round(timeLeft.TotalHours)} год.</p>
                        <p><strong>Опис:</strong> {task.Description}</p>";

                        var success = await emailService.SendEmailAsync(
                            toEmail: task.User.Email,
                            toName: task.User.Username,
                            subject: "Нагадування про дедлайн завдання",
                            htmlContent: html
                        );

                        if (success)
                        {
                            task.LastReminderSentAt = now;

                            if (!stoppingToken.IsCancellationRequested)
                            {
                                try
                                {
                                    _logger.LogInformation($"✅ Email надіслано автоматично для завдання ID {task.Id}");
                                }
                                catch (ObjectDisposedException) { /* логер вже не активний – ігноруємо */ }
                            }
                        }
                        else
                        {
                            if (!stoppingToken.IsCancellationRequested)
                            {
                                try
                                {
                                    _logger.LogWarning($"❗ Помилка під час надсилання email (авто) для завдання ID {task.Id}");
                                }
                                catch (ObjectDisposedException) { /* ігноруємо */ }
                            }
                        }

                    }

                    await context.SaveChangesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Помилка у фоні TaskReminderBackgroundService");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }
    }
}
