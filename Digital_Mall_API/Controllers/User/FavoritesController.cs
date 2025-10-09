using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.Entities.Product_Catalog;
using Digital_Mall_API.Models.Entities.User___Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Digital_Mall_API.Controllers.User
{
    [Route("User/[controller]")]
    [ApiController]
    public class FavoritesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public FavoritesController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost("toggle")]
        public async Task<IActionResult> ToggleFavorite(int productId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
                return Unauthorized("User not logged in");

            // التأكد من وجود المنتج
            var productExists = await _context.Products.AnyAsync(p => p.Id == productId);
            if (!productExists)
                return NotFound("Product not found");

            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId.ToString() == userId && f.ProductId == productId);

            if (favorite != null)
            {
                _context.Favorites.Remove(favorite);
                await _context.SaveChangesAsync();
                return Ok(new { added = false, message = "Removed from favorites" });
            }

            _context.Favorites.Add(new Favorite
            {
                UserId = Guid.Parse(userId),
                ProductId = productId
            });
            await _context.SaveChangesAsync();

            return Ok(new { added = true, message = "Added to favorites" });
        }



        [HttpGet]
        public async Task<IActionResult> GetFavorites()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
                return Unauthorized("User not logged in");

            var favorites = await _context.Favorites
                .Where(f => f.UserId.ToString() == userId)
                .Include(f => f.Product)
                    .ThenInclude(p => p.Images)
                .Select(f => new
                {
                    f.ProductId,
                    ProductName = f.Product.Name,
                    ImageUrl = f.Product.Images.FirstOrDefault().ImageUrl
                })
                .ToListAsync();

            return Ok(favorites);
        }

       
        [HttpGet("is-favorite/{productId}")]
        public async Task<IActionResult> IsFavorite(int productId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
                return Unauthorized();

            var exists = await _context.Favorites
                .AnyAsync(f => f.UserId.ToString() == userId && f.ProductId == productId);

            return Ok(new { isFavorite = exists });
        }
    }
}
