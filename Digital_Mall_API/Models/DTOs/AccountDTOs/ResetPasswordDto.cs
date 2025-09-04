using System.ComponentModel.DataAnnotations;

namespace Restaurant_App.Models.DTO
{
    public class ResetPasswordDto
    {
        [Required(ErrorMessage ="Password Requird")]
        public string? Password { get; set; }
        [Compare("Password",ErrorMessage ="DO not match")]
        public string ?ConfirmPassword { get; set; }
        public string? Email { get; set; }
        public string ?Token { get; set; }
    }
}
