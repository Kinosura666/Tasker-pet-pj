using System.ComponentModel.DataAnnotations;

namespace WebGuide.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(30, MinimumLength = 6)]
        public string Username { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? ProfileImageUrl { get; set; } 

    }
}
