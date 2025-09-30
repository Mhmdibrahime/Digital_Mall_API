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
        public string? MobileNumber { get; set; } 
    

    }
    public class RegisterCustomerDto
    {
        [Required] public string Email { get; set; }
        [Required] public string Password { get; set; }
        [Required]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }
        [Required] public string FullName { get; set; }
        public string? MobileNumber { get; set; }
    }

    public class RegisterBrandDto
    {
        [Required] public string Email { get; set; }
        [Required] public string Password { get; set; }

        [Required]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }

        [Required] public string OfficialName { get; set; }
        [Required] public bool Ofline { get; set; }
        [Required] public bool Online { get; set; }
        [Required] public string Facebook { get; set; }
        [Required] public string Instgram { get; set; }
        [Required] public IFormFile EvidenceOfProof { get; set; }
    }

    public class RegisterModelDto
    {
        [Required] public string Email { get; set; }
        [Required] public string Password { get; set; }
        [Required]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }
        [Required] public string ModelName { get; set; }
        [Required] public string PhoneNumber { get; set; }
        [Required] public IFormFile PersonalProof { get; set; }
    }
}
