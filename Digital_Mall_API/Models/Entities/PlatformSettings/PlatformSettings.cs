using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.Entities.PlatformSettings
{
    public class PlatformSettings
    {
        public int Id { get; set; }

        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(1000)]
        public string LogoUrl { get; set; }

        [StringLength(200)]
        public string SupportEmail { get; set; }

        [StringLength(20)]
        public string SupportPhone { get; set; }
    }
}
