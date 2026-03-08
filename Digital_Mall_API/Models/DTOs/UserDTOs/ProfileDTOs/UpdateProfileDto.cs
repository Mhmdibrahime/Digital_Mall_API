using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.DTOs.UserDTOs.ProfileDTOs
{
    public class UpdateProfileDto
    {
        [StringLength(100)]
        public string FullName { get; set; }

        [StringLength(20)]
        public string PhoneNumber { get; set; }

        [EmailAddress]
        public string Email { get; set; }
    }
    public class UpgradeToBrandDto
    {
        [Required]
        [StringLength(200)]
        public string OfficialName { get; set; }

   
        public bool Online { get; set; }

        public bool Offline { get; set; }

        [Required]
        public IFormFile EvidenceFile { get; set; } // Commercial register or tax card image



        [StringLength(200)]
        public string? Facebook { get; set; }

        [StringLength(200)]
        public string? Instagram { get; set; }

        [StringLength(500)]
        public string? Location { get; set; }


      
        
 
    }
    public class UpgradeToModelDto
    {
        [Required]
        [StringLength(500)]
        public string Name { get; set; }

        

        [Required]
        public IFormFile EvidenceFile { get; set; } // Proof of identity or portfolio

       

      

     
    }
}
