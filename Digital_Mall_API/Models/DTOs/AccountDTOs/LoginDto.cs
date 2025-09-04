using System.ComponentModel.DataAnnotations;

namespace Academic.Models.Dto
{
    public class LoginDto
    {
        [EmailAddress]
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
