using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebGuide.Data;
using WebGuide.Models;
using WebGuide.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.Fonts;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

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

        [HttpGet("/Statistics/ChartPie")]
        public async Task<IActionResult> GetChart()
        {
            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            var allTasks = await _context.Tasks.Where(t => t.UserId == user.Id).ToListAsync();
            var completed = allTasks.Count(t => t.IsCompleted);
            var overdue = allTasks.Count(t => !t.IsCompleted && t.Deadline < DateTime.UtcNow);
            var active = allTasks.Count - completed - overdue;

            var chartBytes = GeneratePieChartImageSharp(completed, overdue, active);
            return File(chartBytes, "image/png");
        }

        [HttpGet("/Statistics/ChartBar")]
        public async Task<IActionResult> ChartBar()
        {
            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            var allTasks = await _context.Tasks.Where(t => t.UserId == user.Id).ToListAsync();

            var data = new Dictionary<string, int>
            {
                { "Очікують", allTasks.Count(t => !t.IsCompleted && t.Deadline > DateTime.UtcNow) },
                { "Виконано", allTasks.Count(t => t.IsCompleted) },
                { "Прострочено", allTasks.Count(t => !t.IsCompleted && t.Deadline <= DateTime.UtcNow) }
            };

            var chart = GenerateBarChartImageSharp(data);
            return File(chart, "image/png");
        }


        [HttpGet]
        public async Task<IActionResult> ExportPdf()
        {
            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            var allTasks = await _context.Tasks.Where(t => t.UserId == user.Id).ToListAsync();
            var stats = _statisticsService.Calculate(allTasks, DateTime.UtcNow);

            byte[] pie = GeneratePieChartImageSharp(stats.CompletedTasks, stats.OverdueTasks, stats.TotalTasks - stats.CompletedTasks - stats.OverdueTasks);
            var bar = GenerateBarChartImageSharp(new Dictionary<string, int>
            {
                { "Очікують", allTasks.Count(t => !t.IsCompleted && t.Deadline > DateTime.UtcNow) },
                { "Виконано", allTasks.Count(t => t.IsCompleted) },
                { "Прострочено", allTasks.Count(t => !t.IsCompleted && t.Deadline <= DateTime.UtcNow) }
            });

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(20);
                    page.Size(PageSizes.A4);
                    page.Content().Column(col =>
                    {
                        col.Item().Text("Статистика користувача").FontSize(20).Bold();
                        col.Item().Text($"Завдань: {stats.TotalTasks}");
                        col.Item().Text($"Виконано: {stats.CompletedTasks}");
                        col.Item().Text($"Прострочено: {stats.OverdueTasks}");
                        col.Item().Text($"Виконано (%): {stats.CompletionRate}%");
                        col.Item().Image(pie);
                        col.Item().Image(bar);
                    });
                });
            });

            using var stream = new MemoryStream();
            doc.GeneratePdf(stream);
            stream.Seek(0, SeekOrigin.Begin);

            return File(stream.ToArray(), "application/pdf", "statistics.pdf");
        }

        private byte[] GeneratePieChartImageSharp(int completed, int overdue, int active)
        {
            int width = 500, height = 500;
            var image = new Image<Rgba32>(width, height);
            image.Mutate(ctx => ctx.Fill(SixLabors.ImageSharp.Color.White));

            float[] values = { completed, overdue, active };
            string[] labels = { "Виконані", "Прострочені", "Активні" };
            var colors = new[] {
                SixLabors.ImageSharp.Color.Green,
                SixLabors.ImageSharp.Color.Red,
                SixLabors.ImageSharp.Color.Orange
            };

            float total = values.Sum();
            if (total == 0) total = 1;

            var center = new PointF(width / 2f, height / 2f);
            float radius = 150f;
            float startAngle = 0;
            var fontFamily = SystemFonts.Families.FirstOrDefault();
            if (fontFamily.Equals(default(FontFamily)))
            {
                throw new Exception("❌ No fonts found. Install fonts-dejavu in Dockerfile.");
            }


            var font = fontFamily.CreateFont(14);

            for (int i = 0; i < values.Length; i++)
            {
                float sweepAngle = values[i] / total * 360f;

                var pathBuilder = new PathBuilder();
                pathBuilder.MoveTo(center);

                int segments = 50;
                for (int j = 0; j <= segments; j++)
                {
                    float angle = startAngle + (sweepAngle * j / segments);
                    float rad = MathF.PI * angle / 180f;
                    pathBuilder.LineTo(new PointF(
                        center.X + radius * MathF.Cos(rad),
                        center.Y + radius * MathF.Sin(rad)
                    ));
                }

                pathBuilder.CloseFigure();
                image.Mutate(ctx => ctx.Fill(colors[i], pathBuilder.Build()));

                startAngle += sweepAngle;
            }

            for (int i = 0; i < labels.Length; i++)
            {
                float y = 370 + i * 22;
                image.Mutate(ctx => {
                    ctx.Fill(colors[i], new RectangleF(20, y, 14, 14));
                    ctx.DrawText($"{labels[i]}: {values[i]}", font, SixLabors.ImageSharp.Color.Black, new PointF(40, y - 2));
                });
            }

            if (values.All(v => v == 0))
            {
                image.Mutate(ctx => ctx.DrawText("Немає даних", font, SixLabors.ImageSharp.Color.Gray, new PointF(150, 200)));
            }

            using var ms = new MemoryStream();
            image.SaveAsPng(ms);
            return ms.ToArray();
        }

        private byte[] GenerateBarChartImageSharp(Dictionary<string, int> data)
        {
            int width = 500, height = 400;
            var image = new Image<Rgba32>(width, height);
            image.Mutate(ctx => ctx.Fill(SixLabors.ImageSharp.Color.White));

            int barWidth = 80, spacing = 40, maxHeight = 200;
            int max = data.Values.Any() ? data.Values.Max() : 1;
            int x = 50;

            var fontFamily = SystemFonts.Families.FirstOrDefault();
            if (fontFamily.Equals(default(FontFamily)))
            {
                throw new Exception("❌ No fonts found. Install fonts-dejavu in Dockerfile.");
            }
;

            var font = fontFamily.CreateFont(14);

            var colors = new[] {
                SixLabors.ImageSharp.Color.Orange,
                SixLabors.ImageSharp.Color.Green,
                SixLabors.ImageSharp.Color.Red
            };

            int i = 0;
            foreach (var item in data)
            {
                int barHeight = (int)(item.Value / (float)max * maxHeight);
                int y = height - barHeight - 60;

                image.Mutate(ctx =>
                {
                    ctx.Fill(colors[i], new Rectangle(x, y, barWidth, barHeight));
                    ctx.DrawText(item.Value.ToString(), font, SixLabors.ImageSharp.Color.Black, new PointF(x + 10, y - 20));
                    ctx.DrawText(item.Key, font, SixLabors.ImageSharp.Color.Black, new PointF(x, height - 40));
                });

                x += barWidth + spacing;
                i++;
            }

            if (data.Values.All(v => v == 0))
            {
                image.Mutate(ctx => ctx.DrawText("Немає даних", font, SixLabors.ImageSharp.Color.Gray, new PointF(150, 200)));
            }

            using var ms = new MemoryStream();
            image.SaveAsPng(ms);
            return ms.ToArray();
        }


    }
}
