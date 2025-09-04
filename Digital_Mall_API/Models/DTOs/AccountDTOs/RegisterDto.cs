using System.ComponentModel.DataAnnotations;

namespace Academic.Models.Dto
{
    public class RegisterDto
    {

        [Required]
        public string Role { get; set; } 

        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required, EmailAddress]
        public string Email { get; set; }
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }

        public string? Username { get; set; } 
        public string? MobileNumber { get; set; } // For instructor
    

    }
}
