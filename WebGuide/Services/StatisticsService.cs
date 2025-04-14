using WebGuide.Models;

namespace WebGuide.Services
{
    public class StatisticsService
    {
        public Statistics Calculate(List<TaskEntity> tasks, DateTime now)
        {
            var completed = tasks.Count(t => t.IsCompleted);
            var overdue = tasks.Count(t => !t.IsCompleted && t.Deadline < now);
            var upcoming3d = tasks.Count(t => !t.IsCompleted && t.Deadline > now && t.Deadline <= now.AddDays(3));
            var upcoming7d = tasks.Count(t => !t.IsCompleted && t.Deadline > now && t.Deadline <= now.AddDays(7));
            var upcoming30d = tasks.Count(t => !t.IsCompleted && t.Deadline > now && t.Deadline <= now.AddDays(30));

            var total = tasks.Count;
            var rate = total > 0 ? Math.Round((double)completed / total * 100, 2) : 0;

            return new Statistics
            {
                TotalTasks = total,
                CompletedTasks = completed,
                OverdueTasks = overdue,
                TasksNext3Days = upcoming3d,
                TasksNext7Days = upcoming7d,
                TasksNext30Days = upcoming30d,
                CompletionRate = rate
            };
        }

    }
}
