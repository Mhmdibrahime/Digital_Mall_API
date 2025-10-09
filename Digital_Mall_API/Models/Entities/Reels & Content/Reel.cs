using Digital_Mall_API.Models.Entities.User___Authentication;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Digital_Mall_API.Models.Entities.Reels___Content
{
    public class Reel
    {
        public int Id { get; set; }

        [Required]
        public string PostedByUserId { get; set; }

        [StringLength(20)]
        public string? PostedByUserType { get; set; }

        [Required]
        [StringLength(500)]
        public string VideoUrl { get; set; }

        [Required]
        [StringLength(500)]
        public string ThumbnailUrl { get; set; }

        [StringLength(2000)]
        public string? Caption { get; set; }

        [Required]
        public DateTime PostedDate { get; set; }

        [Required]
        [Range(1, 300)]
        public int DurationInSeconds { get; set; }

        public int LikesCount { get; set; } = 0;
        public int SharesCount { get; set; } = 0;

        [StringLength(100)]
        public string? MuxUploadId { get; set; }

        [StringLength(100)]
        public string? MuxAssetId { get; set; }

        [StringLength(100)]
        public string? MuxPlaybackId { get; set; }

        [StringLength(50)]
        public string UploadStatus { get; set; } = "draft";
        public string? UploadError { get; set; }

        [JsonIgnore]
        public string? TemporaryUploadUrl { get; set; }

        public string? PostedByBrandId { get; set; }
        public string? PostedByModelId { get; set; }

        public virtual Brand? PostedByBrand { get; set; }
        public virtual FashionModel? PostedByModel { get; set; }

        public virtual List<ReelProduct>? LinkedProducts { get; set; } = new List<ReelProduct>();
    }
}
