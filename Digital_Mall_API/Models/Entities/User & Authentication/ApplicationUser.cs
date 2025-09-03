using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Models.Entities.User___Authentication
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        [StringLength(100)]
        public string? DisplayName { get; set; }

        [Url]
        [StringLength(500)]
        public string? ProfilePictureUrl { get; set; }

        public virtual Customer? CustomerProfile { get; set; }
        public virtual Brand? BrandProfile { get; set; }
        public virtual FashionModel? ModelProfile { get; set; }
        public virtual TshirtDesigner? DesignerProfile { get; set; }
    }
}