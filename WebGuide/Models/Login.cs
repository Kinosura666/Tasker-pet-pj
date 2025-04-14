using System.ComponentModel.DataAnnotations;
using WebGuide.Attributes;

namespace WebGuide.Models
{
    public class Login
    {
        [Required(ErrorMessage = "You must fill this field")]
        [Display(Name ="Email")]
        [BetterEmail(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }

        [Required(ErrorMessage = "You must fill this field")]
        [Display(Name = "Password")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
