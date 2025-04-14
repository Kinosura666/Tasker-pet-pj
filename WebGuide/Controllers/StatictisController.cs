using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Drawing;
using System.Security.Claims;
using WebGuide.Data;
using System.Drawing.Imaging;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using WebGuide.Models;
using WebGuide.Services;
using SkiaSharp;

namespace WebGuide.Controllers
{
    [Authorize]
    public class StatisticsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly StatisticsService _statisticsService;

        public StatisticsController(ApplicationDbContext context, StatisticsService statisticsService)
        {
            _context = context;
            _statisticsService = statisticsService;
        }

        public async Task<IActionResult> Index()
        {
            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            var allTasks = await _context.Tasks.Where(t => t.UserId == user.Id).ToListAsync();

            var stats = _statisticsService.Calculate(allTasks, DateTime.UtcNow);

            ViewBag.Total = stats.TotalTasks;
            ViewBag.Completed = stats.CompletedTasks;
            ViewBag.Overdue = stats.OverdueTasks;
            ViewBag.Upcoming3d = stats.TasksNext3Days;
            ViewBag.Upcoming7d = stats.TasksNext7Days;
            ViewBag.Upcoming30d = stats.TasksNext30Days;
            ViewBag.CompletionPercent = stats.CompletionRate;

            return View(stats);
        }

        [HttpGet("/Statistics/Chart")]
        public async Task<IActionResult> GetChart()
        {
            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            var total = await _context.Tasks.CountAsync(t => t.UserId == user.Id);
            var completed = await _context.Tasks.CountAsync(t => t.UserId == user.Id && t.IsCompleted);
            var overdue = await _context.Tasks.CountAsync(t => t.UserId == user.Id && !t.IsCompleted && t.Deadline < DateTime.UtcNow);
            var active = total - completed - overdue;

            string[] labels = { "Виконані", "Прострочені", "Активні" };
            int[] values = { completed, overdue, active };
            Color[] colors = { Color.FromArgb(40, 167, 69), Color.FromArgb(220, 53, 69), Color.FromArgb(255, 193, 7) };

            int width = 400;
            int height = 450;

            using var bmp = new Bitmap(width, height);
            using var gfx = Graphics.FromImage(bmp);
            gfx.Clear(Color.White);

            var totalSum = values.Sum();
            float startAngle = 0f;

            for (int i = 0; i < values.Length; i++)
            {
                float sweep = (float)values[i] / totalSum * 360f;
                using var brush = new SolidBrush(colors[i]);
                gfx.FillPie(brush, 50, 50, 300, 300, startAngle, sweep);
                startAngle += sweep;
            }

            for (int i = 0; i < labels.Length; i++)
            {
                gfx.FillRectangle(new SolidBrush(colors[i]), 20, 370 + i * 20, 12, 12);
                gfx.DrawString($"{labels[i]}: {values[i]}", new System.Drawing.Font("Arial", 10), Brushes.Black, 40, 368 + i * 20);
            }

            using var stream = new MemoryStream();
            bmp.Save(stream, ImageFormat.Png);
            stream.Seek(0, SeekOrigin.Begin);

            return File(stream.ToArray(), "image/png");
        }

        [HttpGet]
        public IActionResult ChartBar()
        {
            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var userId = _context.Users.FirstOrDefault(u => u.Email == email)?.Id;

            var tasks = _context.Tasks.Where(t => t.UserId == userId).ToList();

            var counts = new Dictionary<string, int>
            {
                { "Очікують", tasks.Count(t => !t.IsCompleted && t.Deadline > DateTime.UtcNow) },
                { "Виконано", tasks.Count(t => t.IsCompleted) },
                { "Прострочено", tasks.Count(t => !t.IsCompleted && t.Deadline <= DateTime.UtcNow) }
            };

            var width = 600;
            var height = 450;
            var bitmap = new Bitmap(width, height);
            using var gfx = Graphics.FromImage(bitmap);
            gfx.Clear(Color.White);

            var barWidth = 100;
            var maxBarHeight = 300;
            var spacing = 60;

            var maxValue = counts.Max(c => c.Value);
            var brushColors = new[] { Brushes.Orange, Brushes.Green, Brushes.Red };

            int x = 80;
            int i = 0;
            foreach (var kv in counts)
            {
                var barHeight = maxValue > 0 ? (kv.Value * maxBarHeight / maxValue) : 0;
                var y = height - barHeight - 60;

                gfx.FillRectangle(brushColors[i], x, y, barWidth, barHeight);
                gfx.DrawRectangle(Pens.Black, x, y, barWidth, barHeight);

                gfx.DrawString(kv.Value.ToString(), new Font("Arial", 12, FontStyle.Bold), Brushes.Black, x + 25, y - 20, new StringFormat { Alignment = StringAlignment.Center });

                gfx.DrawString(kv.Key, new Font("Arial", 10), Brushes.Black, x + barWidth / 2, height - 50, new StringFormat { Alignment = StringAlignment.Center });

                x += barWidth + spacing;
                i++;
            }

            using var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Png);
            return File(ms.ToArray(), "image/png");
        }

        [HttpGet]
        public async Task<IActionResult> ExportPdf()
        {
            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            var now = DateTime.UtcNow;

            var allTasks = await _context.Tasks.Where(t => t.UserId == user.Id).ToListAsync();
            var completed = allTasks.Count(t => t.IsCompleted);
            var overdue = allTasks.Count(t => !t.IsCompleted && t.Deadline < now);
            var upcoming3d = allTasks.Count(t => !t.IsCompleted && t.Deadline > now && t.Deadline <= now.AddDays(3));
            var upcoming7d = allTasks.Count(t => !t.IsCompleted && t.Deadline > now && t.Deadline <= now.AddDays(7));
            var upcoming30d = allTasks.Count(t => !t.IsCompleted && t.Deadline > now && t.Deadline <= now.AddDays(30));
            var completionPercent = allTasks.Count == 0 ? 0 : (int)((double)completed / allTasks.Count * 100);

            var document = new PdfDocument();
            var page = document.AddPage();
            var gfx = XGraphics.FromPdfPage(page);
            var font = new XFont("DejaVuSans", 12);

            int y = 40;
            gfx.DrawString("📈 Статистика користувача", new XFont("DejaVuSans", 16, XFontStyle.Bold), XBrushes.Black, new XPoint(40, y));
            y += 40;

            gfx.DrawString($"Загальна кількість завдань: {allTasks.Count}", font, XBrushes.Black, new XPoint(40, y += 25));
            gfx.DrawString($"Виконано: {completed}", font, XBrushes.Black, new XPoint(40, y += 25));
            gfx.DrawString($"Прострочено: {overdue}", font, XBrushes.Black, new XPoint(40, y += 25));
            gfx.DrawString($"Спливають за 3 дні: {upcoming3d}", font, XBrushes.Black, new XPoint(40, y += 25));
            gfx.DrawString($"Спливають а 7 днів: {upcoming7d}", font, XBrushes.Black, new XPoint(40, y += 25));
            gfx.DrawString($"Спливають за 30 днів: {upcoming30d}", font, XBrushes.Black, new XPoint(40, y += 25));
            gfx.DrawString($"Виконано (%): {completionPercent}%", font, XBrushes.Black, new XPoint(40, y += 25));

            byte[] pieImage = GeneratePieChart(completed, overdue, allTasks.Count - completed - overdue);
            byte[] barImage = GenerateBarChart(allTasks);

            var pieXImage = XImage.FromStream(() => new MemoryStream(pieImage));
            gfx.DrawImage(pieXImage, 40, y += 40, 250, 250);

            var barXImage = XImage.FromStream(() => new MemoryStream(barImage));
            gfx.DrawImage(barXImage, 310, y - 10, 250, 250);

            using var stream = new MemoryStream();
            document.Save(stream, false);
            stream.Seek(0, SeekOrigin.Begin);
            return File(stream.ToArray(), "application/pdf", "statistics.pdf");
        }

        private byte[] GeneratePieChart(int completed, int overdue, int active)
        {
            var values = new[] { completed, overdue, active };
            var labels = new[] { "Виконані", "Прострочені", "Активні" };
            var colors = new[] {
                new SKColor(40, 167, 69),
                new SKColor(220, 53, 69),
                new SKColor(255, 193, 7)
                             };

            var imageInfo = new SKImageInfo(500, 500);
            using var surface = SKSurface.Create(imageInfo);
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);

            float total = values.Sum();
            float startAngle = 0;

            var center = new SKPoint(250, 250);
            var radius = 150;

            for (int i = 0; i < values.Length; i++)
            {
                float sweep = (float)values[i] / total * 360f;
                using var paint = new SKPaint { Style = SKPaintStyle.Fill, Color = colors[i] };
                canvas.DrawArc(new SKRect(100, 100, 400, 400), startAngle, sweep, true, paint);
                startAngle += sweep;
            }

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }


        private byte[] GenerateBarChart(List<TaskEntity> tasks)
        {
            var counts = new Dictionary<string, int>
            {
                { "Очікують", tasks.Count(t => !t.IsCompleted && t.Deadline > DateTime.UtcNow) },
                { "Виконано", tasks.Count(t => t.IsCompleted) },
                { "Прострочено", tasks.Count(t => !t.IsCompleted && t.Deadline <= DateTime.UtcNow) }
            };

            var colors = new[] { SKColors.Orange, SKColors.Green, SKColors.Red };
            var imageInfo = new SKImageInfo(600, 400);
            using var surface = SKSurface.Create(imageInfo);
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);

            int max = counts.Values.Max();
            int barWidth = 100, spacing = 60, x = 60;
            int i = 0;

            foreach (var kv in counts)
            {
                float height = max > 0 ? kv.Value * 200 / (float)max : 0;
                float y = 300 - height;

                using var paint = new SKPaint { Color = colors[i], Style = SKPaintStyle.Fill };
                canvas.DrawRect(x, y, barWidth, height, paint);

                using var textPaint = new SKPaint { Color = SKColors.Black, TextSize = 18 };
                canvas.DrawText(kv.Key, x, 320, textPaint);
                canvas.DrawText(kv.Value.ToString(), x, y - 10, textPaint);

                x += barWidth + spacing;
                i++;
            }

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }


    }
}
