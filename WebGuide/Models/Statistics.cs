namespace WebGuide.Models
{
    public class Statistics
    {
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int OverdueTasks { get; set; }

        public int TasksNext3Days { get; set; }
        public int TasksNext7Days { get; set; }
        public int TasksNext30Days { get; set; }

        public double CompletionRate { get; set; }
    }
}
