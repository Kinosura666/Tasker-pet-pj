using Xunit;
using System;
using System.Collections.Generic;
using WebGuide.Services;
using WebGuide.Models;

namespace WebGuide.Tests.Services
{
    public class StatisticsServiceTests
    {
        [Fact]
        public void Calculate_ReturnsCorrectStatistics()
        {
            var now = new DateTime(2025, 4, 1);
            var tasks = new List<TaskEntity>
            {
                new TaskEntity { IsCompleted = true },                                   // 1 completed
                new TaskEntity { IsCompleted = false, Deadline = now.AddDays(-1) },      // 1 overdue
                new TaskEntity { IsCompleted = false, Deadline = now.AddDays(2) },       // 1 in 3 days
                new TaskEntity { IsCompleted = false, Deadline = now.AddDays(6) },       // 1 in 7 days
                new TaskEntity { IsCompleted = false, Deadline = now.AddDays(20) },      // 1 in -30 days
                new TaskEntity { IsCompleted = false, Deadline = now.AddDays(40) }       // 1 in 30+ days
            };

            var service = new StatisticsService();

            var result = service.Calculate(tasks, now);


            Assert.Equal(6, result.TotalTasks);
            Assert.Equal(1, result.CompletedTasks);
            Assert.Equal(1, result.OverdueTasks);
            Assert.Equal(1, result.TasksNext3Days);
            Assert.Equal(2, result.TasksNext7Days);     // 3d + 7d
            Assert.Equal(3, result.TasksNext30Days);    // 3d + 7d + 20-30d
        }

        [Fact]
        public void Calculate_ShouldReturnZeroCompletionRate_WhenNoTasks()
        {
            var tasks = new List<TaskEntity>();
            var service = new StatisticsService();

            var result = service.Calculate(tasks, DateTime.Now);

            Assert.Equal(0, result.CompletionRate);
        }
    }
}
