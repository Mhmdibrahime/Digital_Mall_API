using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.Entities;
using Digital_Mall_API.Models.DTOs.SuperAdminDTOs.RefundDTOs;
using Digital_Mall_API.Models.Entities.Orders___Shopping;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Digital_Mall_API.Controllers.SuperAdmin
{
    [ApiController]
    [Route("Super/[controller]")]
    public class RefundsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public RefundsController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpGet("GetRefundRequests")]
        public async Task<ActionResult> GetRefundRequests([FromQuery] string search = "")
        {
            try
            {
                var query = _context.RefundRequests
                    .Include(r => r.Order)
                    .Include(r => r.OrderItem)
                        .ThenInclude(oi => oi.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                    .Include(r => r.OrderItem)
                        .ThenInclude(oi => oi.Brand)
                    .Include(r => r.Customer)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(r =>
                        r.OrderId.ToString().Contains(search) ||
                        r.RefundNumber.Contains(search) ||
                        r.Customer.FullName.Contains(search) ||
                        r.OrderItem.Brand.OfficialName.Contains(search));
                }

                var refunds = await query
                    .OrderByDescending(r => r.RequestDate)
                    .Select(r => new RefundRequestDto
                    {
                        Id = r.Id,
                        RefundNumber = r.RefundNumber,
                        OrderId = r.OrderId,
                        OrderItemId = r.OrderItemId,
                        CustomerName = r.Customer.FullName,
                        BrandName = r.OrderItem.Brand.OfficialName,
                        ProductName = r.OrderItem.ProductVariant.Product.Name,
                        OrderDate = r.Order.OrderDate.ToString("yyyy-MM-dd"),
                        Reason = r.Reason,
                        ImageUrl = r.ImageUrl,
                        Status = r.Status,
                        RequestDate = r.RequestDate.ToString("yyyy-MM-dd")
                    })
                    .ToListAsync();

                return Ok(refunds);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving refund requests: {ex.Message}");
            }
        }

        [HttpGet("GetRefundDetails/{id}")]
        public async Task<ActionResult<RefundDetailsDto>> GetRefundDetails(int id)
        {
            try
            {
                var refund = await _context.RefundRequests
                    .Include(r => r.Order)
                    .Include(r => r.OrderItem)
                        .ThenInclude(oi => oi.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                    .Include(r => r.OrderItem)
                        .ThenInclude(oi => oi.Brand)
                    .Include(r => r.Customer)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (refund == null)
                {
                    return NotFound("Refund request not found");
                }

                var refundDetails = new RefundDetailsDto
                {
                    Id = refund.Id,
                    RefundNumber = refund.RefundNumber,
                    OrderId = refund.OrderId,
                    OrderDate = refund.Order.OrderDate.ToString("yyyy-MM-dd"),
                    CustomerName = refund.Customer.FullName,
                    CustomerEmail = refund.Customer.Email,
                    BrandName = refund.OrderItem.Brand.OfficialName,
                    ProductName = refund.OrderItem.ProductVariant.Product.Name,
                    ProductVariant = $"{refund.OrderItem.ProductVariant.Size} - {refund.OrderItem.ProductVariant.Color}",
                    PriceAtPurchase = refund.OrderItem.PriceAtTimeOfPurchase,
                    Quantity = refund.OrderItem.Quantity,
                    Reason = refund.Reason,
                    ImageUrl = refund.ImageUrl,
                    Status = refund.Status,
                    RequestDate = refund.RequestDate.ToString("yyyy-MM-dd"),
                    AdminNotes = refund.AdminNotes
                };

                return Ok(refundDetails);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving refund details: {ex.Message}");
            }
        }

        [HttpPost("{id}/process")]
        public async Task<ActionResult> ProcessRefund(int id, [FromBody] ProcessRefundDto processDto)
        {
            try
            {
                var refund = await _context.RefundRequests
                    .Include(r => r.OrderItem)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (refund == null)
                {
                    return NotFound("Refund request not found");
                }

                if (refund.Status != "Pending")
                {
                    return BadRequest("Refund request has already been processed");
                }

                if (processDto.Status != "Approved" && processDto.Status != "Rejected")
                {
                    return BadRequest("Status must be either 'Approved' or 'Rejected'");
                }

                refund.Status = processDto.Status;
                refund.AdminNotes = processDto.AdminNotes;
                refund.ProcessedDate = DateTime.UtcNow;

                // If approved, you might want to update order status or process payment refund
                if (processDto.Status == "Approved")
                {
                    // Here you can add logic to process the actual refund payment
                    // For example: update order status, process payment reversal, etc.
                    // refund.OrderItem.Order.Status = "Refunded";
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = $"Refund request has been {processDto.Status.ToLower()}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error processing refund: {ex.Message}");
            }
        }

        [HttpDelete("DeleteRefundRequest/{id}")]
        public async Task<ActionResult> DeleteRefundRequest(int id)
        {
            try
            {
                var refund = await _context.RefundRequests.FindAsync(id);

                if (refund == null)
                {
                    return NotFound("Refund request not found");
                }

                if (!string.IsNullOrEmpty(refund.ImageUrl))
                {
                    var imagePath = Path.Combine(_environment.WebRootPath, refund.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                _context.RefundRequests.Remove(refund);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Refund request deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error deleting refund request: {ex.Message}");
            }
        }

       
    }
}