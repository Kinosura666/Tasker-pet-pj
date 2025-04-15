using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebGuide.Data;
using WebGuide.Models;
using WebGuide.Services;

namespace WebGuide.Controllers
{
    [Authorize]
    public class TasksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly TrelloService _trelloService;
        private readonly ILogger<TasksController> _logger;

        public TasksController(ApplicationDbContext context, TrelloService trelloService, ILogger<TasksController> logger)
        {
            _context = context;
            _trelloService = trelloService;
            _logger = logger;
        }

        // GET: Tasks/Index
        public async Task<IActionResult> Index(string? sortBy, int? priorityFilter, [FromQuery] string? showCompleted)
        {
            var showCompletedBool = showCompleted == "true";
            ViewBag.ShowCompleted = showCompletedBool;

            var currentUserEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            var tasksQuery = _context.Tasks
                .Include(t => t.User)
                .Where(t => t.User.Email == currentUserEmail)
                .AsQueryable();

            if (priorityFilter.HasValue)
                tasksQuery = tasksQuery.Where(t => t.Priority == priorityFilter.Value);

            if (!showCompletedBool)
            {
                var now = DateTime.UtcNow;
                tasksQuery = tasksQuery.Where(t => !t.IsCompleted && t.Deadline > now);
            }


            tasksQuery = sortBy switch
            {
                "priorityAsc" => tasksQuery.OrderByDescending(t => t.Priority),
                "priorityDesc" => tasksQuery.OrderBy(t => t.Priority),
                _ => tasksQuery.OrderBy(t => t.Deadline)
            };

            ViewBag.SortBy = sortBy;
            ViewBag.PriorityFilter = priorityFilter;

            var tasks = await tasksQuery.ToListAsync();
            return View(tasks);
        }

        // GET: Tasks/Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var task = await _context.Tasks
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                return NotFound();

            return View(task);
        }

        // GET: Tasks/Create
        [HttpGet("Create")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Tasks/Create
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]  //comment for TaskIntegrTest CreateTask_Should_SaveTaskInDatabase
        public async Task<IActionResult> Create(TaskEntity model, IFormFile? attachment)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .SelectMany(kv => kv.Value.Errors.Select(err => $"[{kv.Key}] {err.ErrorMessage}"))
                    .ToList();

                Console.WriteLine(" ModelState.Errors:");
                foreach (var err in errors)
                    Console.WriteLine(err);
            }


            // UserID = current user
            var currentUserEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == currentUserEmail);
            if (currentUser != null)
            {
                model.UserId = currentUser.Id;
            }
            else
            {
                ModelState.AddModelError("", "Не вдалося визначити користувача.");
                return View(model);
            }

            //Google Cloud Storage
            if (attachment != null && attachment.Length > 0)
            {
                var googleStorageService = HttpContext.RequestServices.GetService<GoogleCloudStorageService>();
                using (var stream = attachment.OpenReadStream())
                {
                    var fileUrl = await googleStorageService.UploadFileAsync(stream, attachment.FileName, attachment.ContentType);
                    model.FileUrl = fileUrl;
                }
            }
            var kyivZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Kyiv");
            model.Deadline = TimeZoneInfo.ConvertTimeToUtc(model.Deadline, kyivZone);
            _context.Tasks.Add(model);
            await _context.SaveChangesAsync();

            //Trello
            try
            {
                var trelloSuccess = await _trelloService.CreateCardAsync(model);
                if (!trelloSuccess)
                {
                    _logger.LogError("Не вдалося створити картку Trello для завдання з ID {TaskId}", model.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Виникла помилка при створенні картки Trello для завдання з ID {TaskId}", model.Id);
            }

            //Mailjet
            try
            {
                var mailjetService = HttpContext.RequestServices.GetService<MailjetEmail>();

                var subject = "Нове завдання створено";
                var html = $@"
                    <h2>📌 Нова задача: <strong>{model.Title}</strong></h2>
                    <p><strong>Опис:</strong> {model.Description}</p>
                    <p><strong>Дедлайн:</strong> {model.Deadline:g}</p>
                    {(string.IsNullOrEmpty(model.FileUrl) ? "" : $"<p><strong>Файл:</strong> <a href='{model.FileUrl}'>Переглянути</a></p>")}
                        ";
                _logger.LogWarning(" Пошта має бути надіслана на {Email}", currentUser.Email);
                await mailjetService.SendEmailAsync(
                    toEmail: currentUser.Email,
                    toName: currentUser.Username,
                    subject: subject,
                    htmlContent: html
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Помилка під час надсилання email через Mailjet.");
            }

            // Google Calendar
            if (model.AddToGoogleCalendar)
            {
                var accessToken = HttpContext.Session.GetString("GoogleAccessToken");

                if (!string.IsNullOrEmpty(accessToken))
                {
                    try
                    {
                        var calendarService = HttpContext.RequestServices.GetRequiredService<GoogleCalendarService>();
                        await calendarService.AddTaskToCalendarAsync(model, accessToken);
                        _logger.LogInformation("Подію додано в Google Календар");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Помилка при додаванні події до Google Календаря");
                    }
                }
                else
                {
                    _logger.LogWarning("Google токен відсутній — подію не додано до календаря");
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Tasks/Edit
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
                return NotFound();

            return View(task);
        }

        // POST: Tasks/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TaskEntity model)
        {
            if (id != model.Id)
                return NotFound();

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var existingTask = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id);
                if (existingTask == null)
                    return NotFound();

                existingTask.Title = model.Title;
                existingTask.Description = model.Description;
                existingTask.Deadline = DateTime.SpecifyKind(model.Deadline, DateTimeKind.Utc); 
                existingTask.Priority = model.Priority;


                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TaskExists(model.Id))
                    return NotFound();
                else
                    throw;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]  //comment for TaskIntegrTest Tasks_Complete_Should_SetIsCompletedToTrue
        public async Task<IActionResult> Complete(int id)
        {
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id);
            
            if (task == null)
                return NotFound();

            if (!task.IsCompleted)
            {
                task.IsCompleted = true;
                task.CompletedAt = DateTime.UtcNow;
                var completedLocal = task.CompletedAt?.ToLocalTime();

                _logger.LogInformation("✅ Завдання ID {Id} позначене як виконане о {Time}", task.Id, completedLocal);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Tasks/Delete
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var task = await _context.Tasks
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (task == null)
                return NotFound();

            return View(task);
        }

        // POST: Tasks/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task != null)
            {
                _context.Tasks.Remove(task);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool TaskExists(int id)
        {
            return _context.Tasks.Any(e => e.Id == id);
        }

        [HttpGet]
        public IActionResult Calendar()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetEvents()
        {
            var currentUserEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var tasks = await _context.Tasks
                .Include(t => t.User)
                .Where(t => t.User.Email == currentUserEmail)
                .ToListAsync();

            var now = DateTime.UtcNow;

            var events = tasks.Select(t => new
            {
                title = t.Title,
                start = t.Deadline.ToString("yyyy-MM-ddTHH:mm:ss"),
                url = Url.Action("Details", "Tasks", new { id = t.Id }),
                backgroundColor = t.IsCompleted ? "#6c757d" : 
                                  t.Deadline < now ? "#f88383" : 
                                  t.Priority switch
                                  {
                                      1 => "#ff0000", 
                                      2 => "#fd7e14", 
                                      _ => "#198754"  
                                  },
                textColor = "#ffffff"
            });

            return Json(events);
        }

    }
}
