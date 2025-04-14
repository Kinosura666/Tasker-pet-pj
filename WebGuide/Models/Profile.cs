using System.ComponentModel.DataAnnotations;

namespace WebGuide.Models
{
    public class ProfileModel
    {
        [Required(ErrorMessage = "Вкажіть ім'я користувача")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Вкажіть Email"), EmailAddress]
        public string Email { get; set; }

        public DateTime CreatedAt { get; set; }

        public int TasksCount { get; set; }

        public IFormFile? ProfileImage { get; set; }
        public string? ExistingImageUrl { get; set; }
    }

}
