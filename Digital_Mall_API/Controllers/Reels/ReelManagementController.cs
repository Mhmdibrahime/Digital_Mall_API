using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.Entities.Product_Catalog;
using Digital_Mall_API.Models.Entities.Reels___Content;
using Digital_Mall_API.Models.Entities.User___Authentication;
using Digital_Mall_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Digital_Mall_API.Controllers.Reels
{
    [ApiController]
    [Route("api/reels")]
    [Authorize]
    public class ReelManagementController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMuxService _muxService;
        private readonly IVimeoService _vimeoService;
        private readonly ILogger<ReelManagementController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReelManagementController(
            AppDbContext context,
            IMuxService muxService,
            IVimeoService vimeoService,
            ILogger<ReelManagementController> logger,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _muxService = muxService;
            _vimeoService = vimeoService;
            _logger = logger;
            _userManager = userManager;
        }

        // Keep existing Mux method
        [HttpPost("prepare-upload/mux")]
        [Authorize]
        public async Task<ActionResult<ReelUploadResponse>> PrepareReelUploadMux([FromBody] PrepareReelUploadRequest request)
        {
            // Your existing Mux code here...
            return await PrepareReelUpload(request, "mux");
        }

        // NEW: Vimeo upload method
        [HttpPost("prepare-upload/vimeo")]
        [Authorize]
        public async Task<ActionResult<ReelUploadResponse>> PrepareReelUploadVimeo([FromBody] PrepareReelUploadRequest request)
        {
            return await PrepareReelUpload(request, "vimeo");
        }

        private async Task<ActionResult<ReelUploadResponse>> PrepareReelUpload(PrepareReelUploadRequest request, string provider)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized(new { error = "User not authenticated" });

                var userRoles = await _userManager.GetRolesAsync(user);
                string? userType = null;
                string? brandId = null;
                string? fashionModelId = null;

                if (userRoles.Contains("FashionModel"))
                {
                    var fashionModel = await _context.FashionModels.FindAsync(user.Id.ToString());
                    if (fashionModel == null)
                        return BadRequest(new { error = "Fashion model profile not found" });

                    userType = "FashionModel";
                    fashionModelId = fashionModel.Id;
                }
                else if (userRoles.Contains("Brand"))
                {
                    var brand = await _context.Brands.FindAsync(user.Id.ToString());
                    if (brand == null)
                        return BadRequest(new { error = "Brand profile not found" });

                    userType = "Brand";
                    brandId = brand.Id;
                }
                else
                {
                    return BadRequest(new { error = "User must be either FashionModel or Brand to upload reels" });
                }

                var reel = new Reel
                {
                    PostedByUserId = user.Id.ToString(),
                    PostedByUserType = userType,
                    PostedByBrandId = brandId,
                    PostedByModelId = fashionModelId,
                    Caption = request.Caption,
                    DurationInSeconds = request.DurationInSeconds,
                    UploadStatus = "draft",
                    PostedDate = DateTime.UtcNow,
                    VideoUrl = "pending",
                    ThumbnailUrl = "pending"
                };

                _context.Reels.Add(reel);
                await _context.SaveChangesAsync(); // Save to get the ID

                if (request.LinkedProductIds?.Any() == true)
                {
                    var existingProducts = await _context.Products
                        .Where(p => request.LinkedProductIds.Contains(p.Id))
                        .Select(p => p.Id)
                        .ToListAsync();

                    var invalidProducts = request.LinkedProductIds.Except(existingProducts).ToList();
                    if (invalidProducts.Any())
                    {
                        _context.Reels.Remove(reel);
                        await _context.SaveChangesAsync();
                        return BadRequest(new { error = $"Invalid product IDs: {string.Join(", ", invalidProducts)}" });
                    }

                    foreach (var productId in existingProducts)
                    {
                        _context.ReelProducts.Add(new ReelProduct
                        {
                            ReelId = reel.Id,
                            ProductId = productId
                        });
                    }

                    await _context.SaveChangesAsync();
                }

                if (provider == "mux")
                {
                    var origin = Request.Headers.Origin.FirstOrDefault() ?? "*";
                    var muxResponse = await _muxService.CreateDirectUploadAsync(reel.Id, origin);

                    reel.MuxUploadId = muxResponse.Data.Id;
                    reel.UploadStatus = "uploading";

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("User {UserId} ({UserType}) created Mux reel {ReelId}", user.Id, userType, reel.Id);

                    return Ok(new ReelUploadResponse
                    {
                        ReelId = reel.Id,
                        UploadUrl = muxResponse.Data.Url,
                        UploadStatus = reel.UploadStatus,
                        PostedByUserType = userType,
                        Provider = "mux"
                    });
                }
                else // vimeo
                {
                    var vimeoResponse = await _vimeoService.CreateVideoUploadAsync(reel.Id);

                    reel.VimeoVideoId = vimeoResponse.VideoId;
                    reel.VimeoUploadUrl = vimeoResponse.UploadUrl;
                    reel.UploadStatus = "uploading";

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("User {UserId} ({UserType}) created Vimeo reel {ReelId}", user.Id, userType, reel.Id);

                    return Ok(new ReelUploadResponse
                    {
                        ReelId = reel.Id,
                        UploadUrl = vimeoResponse.UploadUrl,
                        UploadStatus = reel.UploadStatus,
                        PostedByUserType = userType,
                        Provider = "vimeo",
                        AdditionalData = new
                        {
                            vimeoVideoId = vimeoResponse.VideoId,
                            vimeoUri = vimeoResponse.Uri,
                            formHtml = vimeoResponse.Upload?.Form
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preparing {Provider} upload for user {UserId}",
                    provider, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return StatusCode(500, new { error = $"Failed to prepare {provider} upload" });
            }
        }
        // POST: api/reels/{reelId}/complete-upload
        [HttpPost("{reelId}/complete-upload")]
        [Authorize]
        public async Task<ActionResult<object>> CompleteReelUpload(int reelId)
        {
            try
            {
                // Get the reel
                var reel = await _context.Reels
                    .Where(r => r.Id == reelId)
                    .FirstOrDefaultAsync();

                if (reel == null)
                {
                    return NotFound(new { error = "Reel not found" });
                }

                // Check if user owns this reel
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (reel.PostedByUserId != userId)
                {
                    return Forbid("You don't own this reel");
                }

                // Check if already completed
                if (reel.UploadStatus == "ready")
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Reel is already marked as ready",
                        reel = new
                        {
                            reel.Id,
                            reel.Caption,
                            reel.VideoUrl,
                            reel.ThumbnailUrl,
                            reel.DurationInSeconds,
                            reel.UploadStatus,
                            reel.PostedByUserType
                        }
                    });
                }

                // Check if we have Vimeo video ID
                if (string.IsNullOrEmpty(reel.VimeoVideoId))
                {
                    return BadRequest(new
                    {
                        error = "No Vimeo video associated with this reel",
                        suggestion = "Please upload a video first using the upload URL"
                    });
                }

                // ✅ CRITICAL: Create the player embed URL from Vimeo video ID
                string playerEmbedUrl = $"https://player.vimeo.com/video/{reel.VimeoVideoId}";

                // Update reel with final details
                reel.UploadStatus = "ready";
                reel.VideoUrl = playerEmbedUrl; // Use player embed URL for playback
                reel.VimeoPlayerUrl = playerEmbedUrl; // Store separately if needed

                // Set a default thumbnail if none exists
                if (reel.ThumbnailUrl == "pending" || string.IsNullOrEmpty(reel.ThumbnailUrl))
                {
                    reel.ThumbnailUrl = $"https://i.vimeocdn.com/video/{reel.VimeoVideoId}_640.jpg";
                }


                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    reel = new
                    {
                        reel.Id,
                        reel.Caption,
                        reel.VideoUrl,
                        reel.ThumbnailUrl,
                        reel.DurationInSeconds,
                        reel.UploadStatus,
                        reel.PostedByUserType,
                        vimeoVideoId = reel.VimeoVideoId,
                        vimeoPlayerUrl = reel.VimeoPlayerUrl,
                        
                    },
                    message = "Reel upload completed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing upload for reel {ReelId}", reelId);
                return StatusCode(500, new { error = "Failed to complete upload" });
            }
        }
        // GET: api/reels/{reelId}/check-vimeo-status
        [HttpGet("{reelId}/check-vimeo-status")]
        [Authorize]
        public async Task<ActionResult<object>> CheckVimeoStatus(int reelId)
        {
            try
            {
                var reel = await _context.Reels
                    .Where(r => r.Id == reelId )
                    .FirstOrDefaultAsync();

                if (reel == null)
                {
                    return NotFound(new { error = "Reel not found" });
                }

                // Check if user owns this reel
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (reel.PostedByUserId != userId)
                {
                    return Forbid("You don't own this reel");
                }

                if (string.IsNullOrEmpty(reel.VimeoVideoId))
                {
                    return BadRequest(new { error = "No Vimeo video associated with this reel" });
                }

                // Return the Vimeo video ID and player URL
                return Ok(new
                {
                    success = true,
                    vimeo_video_id = reel.VimeoVideoId,
                    player_embed_url = $"https://player.vimeo.com/video/{reel.VimeoVideoId}",
                    reel_status = reel.UploadStatus,
                    can_complete = reel.UploadStatus != "ready",
                    message = reel.UploadStatus == "ready"
                        ? "Reel is already ready"
                        : "Reel can be marked as ready"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Vimeo status for reel {ReelId}", reelId);
                return StatusCode(500, new { error = "Failed to check Vimeo status" });
            }
        }
        // Add provider field to response
        public class ReelUploadResponse
        {
            public int ReelId { get; set; }
            public string UploadUrl { get; set; }
            public string UploadStatus { get; set; }
            public string PostedByUserType { get; set; }
            public string Provider { get; set; } = "mux"; // Default to mux for backward compatibility
            public object AdditionalData { get; set; }
        }

        // Update GetReelDetails to include Vimeo info
        [HttpGet("{reelId}/details")]
        [Authorize]
        public async Task<ActionResult<ReelDetailDto>> GetReelDetails(int reelId)
        {
            var reel = await _context.Reels
                .Include(r => r.LinkedProducts)
                    .ThenInclude(rp => rp.Product)
                .Include(r => r.PostedByModel)
                .Include(r => r.PostedByBrand)
                .FirstOrDefaultAsync(r => r.Id == reelId);

            if (reel == null)
                return NotFound(new { error = "Reel not found" });

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRoles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);

            var isOwner = reel.PostedByUserId == userId;

            if (!isOwner)
                return Forbid("You don't have permission to view this reel");

            // Determine provider
            string provider = !string.IsNullOrEmpty(reel.VimeoVideoId) ? "vimeo" : "mux";

            var reelDetails = new ReelDetailDto
            {
                Id = reel.Id,
                Caption = reel.Caption,
                VideoUrl = reel.VideoUrl,
                ThumbnailUrl = reel.ThumbnailUrl,
                PostedDate = reel.PostedDate,
                DurationInSeconds = reel.DurationInSeconds,
                LikesCount = reel.LikesCount,
                SharesCount = reel.SharesCount,
                UploadStatus = reel.UploadStatus,
                PostedByUserType = reel.PostedByUserType,
                PostedByName = reel.PostedByUserType == "FashionModel"
                    ? reel.PostedByModel.Name
                    : reel.PostedByBrand.OfficialName,
                LinkedProducts = reel.LinkedProducts.Select(rp => new ReelProductDetailDto
                {
                    ProductId = rp.ProductId,
                    ProductName = rp.Product.Name,
                    ProductPrice = rp.Product.Price,
                    ProductDescription = rp.Product.Description,
                    ProductImageUrl = rp.Product.Images.FirstOrDefault()?.ImageUrl
                }).ToList(),
                Provider = provider,
                MuxAssetId = reel.MuxAssetId,
                MuxPlaybackId = reel.MuxPlaybackId,
                VimeoVideoId = reel.VimeoVideoId,
                VimeoPlayerUrl = reel.VimeoPlayerUrl,
                UploadError = reel.UploadError
            };

            return Ok(reelDetails);
        }

        // Update GetUploadStatus to include Vimeo
        [HttpGet("{reelId}/upload-status")]
        public async Task<ActionResult<ReelUploadStatusResponse>> GetUploadStatus(int reelId)
        {
            var reel = await _context.Reels
                .Include(r => r.LinkedProducts)
                    .ThenInclude(rp => rp.Product)
                .FirstOrDefaultAsync(r => r.Id == reelId);

            if (reel == null) return NotFound();

            string provider = !string.IsNullOrEmpty(reel.VimeoVideoId) ? "vimeo" : "mux";

            return Ok(new ReelUploadStatusResponse
            {
                ReelId = reel.Id,
                UploadStatus = reel.UploadStatus,
                Provider = provider,
                MuxAssetId = reel.MuxAssetId,
                MuxPlaybackId = reel.MuxPlaybackId,
                VimeoVideoId = reel.VimeoVideoId,
                VimeoPlayerUrl = reel.VimeoPlayerUrl,
                VideoUrl = reel.VideoUrl,
                ThumbnailUrl = reel.ThumbnailUrl,
                Error = reel.UploadError,
                PostedByUserType = reel.PostedByUserType
            });
        }

        // Add Vimeo-specific cancellation
        [HttpPost("{reelId}/cancel-upload/vimeo")]
        [Authorize]
        public async Task<ActionResult> CancelVimeoUpload(int reelId)
        {
            var reel = await _context.Reels.FindAsync(reelId);
            if (reel == null) return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (reel.PostedByUserId != userId)
                return Forbid("You can only cancel your own uploads");

            if (reel.UploadStatus == "ready")
                return BadRequest(new { error = "Cannot cancel - upload already completed" });

            // Delete video from Vimeo if it exists
            if (!string.IsNullOrEmpty(reel.VimeoVideoId))
            {
                try
                {
                    await _vimeoService.DeleteVideoAsync(reel.VimeoVideoId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete Vimeo video {VideoId}", reel.VimeoVideoId);
                }
            }

            reel.UploadStatus = "cancelled";
            reel.VimeoVideoId = null;
            reel.VimeoUploadUrl = null;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Vimeo upload cancelled" });
        }
    

    // Update ReelUploadStatusResponse
    public class ReelUploadStatusResponse
    {
        public int ReelId { get; set; }
        public string UploadStatus { get; set; }
        public string Provider { get; set; }
        public string? MuxAssetId { get; set; }
        public string? MuxPlaybackId { get; set; }
        public string? VimeoVideoId { get; set; }
        public string? VimeoPlayerUrl { get; set; }
        public string VideoUrl { get; set; }
        public string ThumbnailUrl { get; set; }
        public string? Error { get; set; }
        public string PostedByUserType { get; set; }
    }

    // Update ReelDetailDto
    public class ReelDetailDto
    {
        public int Id { get; set; }
        public string? Caption { get; set; }
        public string VideoUrl { get; set; }
        public string ThumbnailUrl { get; set; }
        public DateTime PostedDate { get; set; }
        public int DurationInSeconds { get; set; }
        public int LikesCount { get; set; }
        public int SharesCount { get; set; }
        public int ViewsCount { get; set; }
        public string UploadStatus { get; set; }
        public string PostedByUserType { get; set; }
        public string PostedByName { get; set; }
        public string Provider { get; set; }
        public List<ReelProductDetailDto> LinkedProducts { get; set; } = new();
        public string? MuxAssetId { get; set; }
        public string? MuxPlaybackId { get; set; }
        public string? VimeoVideoId { get; set; }
        public string? VimeoPlayerUrl { get; set; }
        public string? UploadError { get; set; }
    }


[HttpGet("products/search")]
        [Authorize]
        public async Task<ActionResult<List<ProductSearchDto>>> SearchProducts(
    [FromQuery] string? search = null,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 50) pageSize = 20;

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var user = await _userManager.GetUserAsync(User);

            IQueryable<Product> query = _context.Products
                .Include(p => p.Brand)
                .Where(p => p.IsActive && p.Brand.Status == "Active"); 


            if (!string.IsNullOrEmpty(search))
            {
                if (int.TryParse(search, out int productId))
                {
                    query = query.Where(p => p.Id == productId || p.Name.Contains(search));
                }
                else
                {
                    query = query.Where(p => p.Name.Contains(search));
                }
            }

            var products = await query
                .OrderBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductSearchDto
                {
                    Id = p.Id,
                    Name = p.Name
                })
                .ToListAsync();

            return Ok(products);
        }
        //[HttpGet("my-reels")]
        //[Authorize]
        //public async Task<ActionResult<List<ReelDto>>> GetMyReels([FromQuery] string? search = null)
        //{
        //    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //    if (string.IsNullOrEmpty(userId))
        //        return Unauthorized();

        //    var query = _context.Reels
        //        .Where(r => r.PostedByUserId == userId)
        //        .Include(r => r.LinkedProducts)
        //            .ThenInclude(rp => rp.Product)
        //        .AsQueryable();

        //    // Apply search filter if provided
        //    if (!string.IsNullOrEmpty(search))
        //    {
        //        query = query.Where(r =>
        //            r.Caption.Contains(search) ||
        //            r.Caption.Contains($"#{search}") ||
        //            r.LinkedProducts.Any(rp => rp.Product.Name.Contains(search))
        //        );
        //    }

        //    var reels = await query
        //        .OrderByDescending(r => r.PostedDate)
        //        .Select(r => new ReelDto
        //        {
        //            Id = r.Id,
        //            Caption = r.Caption,
        //            VideoUrl = r.VideoUrl,
        //            ThumbnailUrl = r.ThumbnailUrl,
        //            PostedDate = r.PostedDate,
        //            DurationInSeconds = r.DurationInSeconds,
        //            LikesCount = r.LikesCount,
        //            SharesCount = r.SharesCount,
        //            UploadStatus = r.UploadStatus,
        //            PostedByUserType = r.PostedByUserType,
        //            LinkedProducts = r.LinkedProducts.Select(rp => new ReelProductDto
        //            {
        //                ProductId = rp.ProductId,
        //                ProductName = rp.Product.Name,
        //                ProductPrice = rp.Product.Price
        //            }).ToList()
        //        })
        //        .ToListAsync();

        //    return Ok(reels);
        //}

        [HttpGet("GetReels")]
        [Authorize]
        public async Task<ActionResult<PagedResponse<ManagementReelDto>>> GetManagementReels(
     [FromQuery] string? search = null,
     [FromQuery] int page = 1,
     [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();
            
            var query = _context.Reels
                .Include(r => r.PostedByModel)
                .Include(r => r.PostedByBrand)
                .Include(r => r.LinkedProducts)
                    .ThenInclude(rp => rp.Product)
                    .Where(x=> x.PostedByUserId == userId && x.UploadStatus == "ready")
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(r =>
                    r.Caption.Contains(search) ||
                    r.Caption.Contains($"#{search}") ||
                    r.PostedByModel.Name.Contains(search) ||
                    r.PostedByBrand.OfficialName.Contains(search) ||
                    r.LinkedProducts.Any(rp => rp.Product.Name.Contains(search))
                );
            }

            var totalCount = await query.CountAsync();

            var reels = await query
                .OrderByDescending(r => r.PostedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new ManagementReelDto
                {
                    Id = r.Id,
                    Caption = r.Caption,
                    VideoUrl = r.VideoUrl,
                    ThumbnailUrl = r.ThumbnailUrl,
                    PostedDate = r.PostedDate,
                    UploadStatus = r.UploadStatus,
                    PostedByUserType = r.PostedByUserType,
                    
                    PostedByName = r.PostedByUserType == "FashionModel"
                        ? r.PostedByModel.Name
                        : r.PostedByBrand.OfficialName,
                    LinkedProductsCount = r.LinkedProducts.Count,
                    LikesCount = r.LikesCount,
                    SharesCount = r.SharesCount,
                })
                .ToListAsync();

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            var hasNextPage = page < totalPages;
            var hasPreviousPage = page > 1;

            var response = new PagedResponse<ManagementReelDto>
            {
                Data = reels,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = hasNextPage,
                HasPreviousPage = hasPreviousPage
            };

            return Ok(response);
        }

        

        [HttpDelete("{reelId}")]
        [Authorize]
        public async Task<ActionResult> DeleteReel(int reelId)
        {
            var reel = await _context.Reels
                .Include(r => r.LinkedProducts)
                .FirstOrDefaultAsync(r => r.Id == reelId);

            if (reel == null)
                return NotFound(new { error = "Reel not found" });

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRoles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);

            var isOwner = reel.PostedByUserId == userId;

            if (!isOwner )
                return Forbid("You don't have permission to delete this reel");

            try
            {
                _context.ReelProducts.RemoveRange(reel.LinkedProducts);
                

                _context.Reels.Remove(reel);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Reel {ReelId} deleted by user {UserId}", reelId, userId);

                return Ok(new { message = "Reel deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting reel {ReelId} by user {UserId}", reelId, userId);
                return StatusCode(500, new { error = "Failed to delete reel" });
            }
        }

        [HttpPatch("{reelId}/status")]
       // [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult> UpdateReelStatus(int reelId, [FromBody] UpdateReelStatusRequest request)
        {
            var reel = await _context.Reels.FindAsync(reelId);
            if (reel == null)
                return NotFound(new { error = "Reel not found" });

            if (string.IsNullOrEmpty(request.Status))
                return BadRequest(new { error = "Status is required" });

            reel.UploadStatus = request.Status.ToLower();
            await _context.SaveChangesAsync();

            _logger.LogInformation("Reel {ReelId} status updated to {Status} by user {UserId}",
                reelId, request.Status, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            return Ok(new { message = $"Reel status updated to {request.Status}" });
        }

       

        [HttpPost("{reelId}/cancel-upload")]
        [Authorize]
        public async Task<ActionResult> CancelUpload(int reelId)
        {
            var reel = await _context.Reels.FindAsync(reelId);
            if (reel == null) return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (reel.PostedByUserId != userId)
                return Forbid("You can only cancel your own uploads");

            if (reel.UploadStatus == "ready")
                return BadRequest(new { error = "Cannot cancel - upload already completed" });

            reel.UploadStatus = "cancelled";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Upload cancelled" });
        }
    }

    public class PrepareReelUploadRequest
    {
        public string? Caption { get; set; }
        public int DurationInSeconds { get; set; }
        public List<int>? LinkedProductIds { get; set; }
    }

    public class ReelUploadResponse
    {
        public int ReelId { get; set; }
        public string UploadUrl { get; set; }
        public string UploadStatus { get; set; }
        public string PostedByUserType { get; set; }
    }

    public class ReelUploadStatusResponse
    {
        public int ReelId { get; set; }
        public string UploadStatus { get; set; }
        public string? MuxAssetId { get; set; }
        public string? MuxPlaybackId { get; set; }
        public string VideoUrl { get; set; }
        public string ThumbnailUrl { get; set; }
        public string? Error { get; set; }
        public string PostedByUserType { get; set; }
    }

    public class ReelDto
    {
        public int Id { get; set; }
        public string? Caption { get; set; }
        public string VideoUrl { get; set; }
        public string ThumbnailUrl { get; set; }
        public DateTime PostedDate { get; set; }
        public int DurationInSeconds { get; set; }
        public int LikesCount { get; set; }
        public int SharesCount { get; set; }
        public int ViewsCount { get; set; }
        public string UploadStatus { get; set; }
        public string PostedByUserType { get; set; }
        public List<ReelProductDto> LinkedProducts { get; set; } = new();
    }

    public class ManagementReelDto
    {
        public int Id { get; set; }
        public string? Caption { get; set; }
        public string VideoUrl { get; set; }
        public string ThumbnailUrl { get; set; }
        public DateTime PostedDate { get; set; }
        public string UploadStatus { get; set; }
        public string PostedByUserType { get; set; }
        public string PostedByName { get; set; }
        public int LinkedProductsCount { get; set; }
        public int LikesCount { get; set; }
        public int SharesCount { get; set; }
        public int ViewsCount { get; set; }
    }

    public class ReelProductDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal ProductPrice { get; set; }
        public string ProductImageUrl { get; set; } 
    }

    public class ReelDetailDto
    {
        public int Id { get; set; }
        public string? Caption { get; set; }
        public string VideoUrl { get; set; }
        public string ThumbnailUrl { get; set; }
        public DateTime PostedDate { get; set; }
        public int DurationInSeconds { get; set; }
        public int LikesCount { get; set; }
        public int SharesCount { get; set; }
        public int ViewsCount { get; set; }
        public string UploadStatus { get; set; }
        public string PostedByUserType { get; set; }
        public string PostedByName { get; set; }
        public List<ReelProductDetailDto> LinkedProducts { get; set; } = new();
        public string? MuxAssetId { get; set; }
        public string? MuxPlaybackId { get; set; }
        public string? UploadError { get; set; }
    }

    public class ReelProductDetailDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal ProductPrice { get; set; }
        public string? ProductDescription { get; set; }
        public string? ProductImageUrl { get; set; }
    }

    public class UpdateReelStatusRequest
    {
        public string Status { get; set; }
    }
    public class PagedResponse<T>
    {
        public List<T> Data { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }
    public class ProductSearchDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}