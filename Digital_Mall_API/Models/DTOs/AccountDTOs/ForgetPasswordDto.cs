using System.ComponentModel.DataAnnotations;

namespace Restaurant_App.Models.DTO
{
    public class ForgetPasswordDto
    {
        [Required]
        [EmailAddress]
        public string? Email { get; set; }
        [Required]
        public string ClientUri { get; set; }

    }
}
