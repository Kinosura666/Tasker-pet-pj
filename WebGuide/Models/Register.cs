using System.ComponentModel.DataAnnotations;
using WebGuide.Attributes; 

namespace WebGuide.Models
{
    public class Register
    {
        [Required(ErrorMessage = "You must fill this field")]
        [Display(Name = "Username")]
        [StringLength(30, MinimumLength = 6, ErrorMessage = "Username must be 6 to 30 symbols")]
        public string Username { get; set; }

        [Required(ErrorMessage = "You must fill this field")]
        [Display(Name = "Email")]
        [BetterEmail(ErrorMessage = "Enter valid email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "You must fill this field")]
        [Display(Name = "Password")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 symbols")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "You must fill this field")]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "Passwords are not same")]
        [DataType(DataType.Password)]
        public string confirmPassword { get; set; }
    }
}
