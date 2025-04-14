using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebGuide.Models
{
    public class TaskEntity
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; }

        public string Description { get; set; }

        public DateTime Deadline { get; set; }

        public int Priority { get; set; }

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        [ValidateNever]
        public User User { get; set; }

        public string? FileUrl { get; set; }

        public DateTime? LastReminderSentAt { get; set; }

        public bool Reminder24hSent { get; set; } = false;
        public bool Reminder12hSent { get; set; } = false;
        public bool Reminder2hSent { get; set; } = false;

        public bool AddToGoogleCalendar { get; set; } = false;

        public bool IsCompleted { get; set; } = false;

        public DateTime? CompletedAt { get; set; }


    }
}
