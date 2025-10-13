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
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var refund = await _context.RefundRequests
                    .Include(r => r.OrderItem)
                        .ThenInclude(oi => oi.ProductVariant)
                    .Include(r => r.Customer)
                    .Include(r => r.Order)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (refund == null)
                {
                    return NotFound("Refund request not found");
                }

                if (refund.Status != "Pending")
                {
                    return BadRequest("Refund request has already been processed");
                }

                // Check if this order item already has an approved refund
                if (processDto.Status == "Approved")
                {
                    var existingApprovedRefund = await _context.RefundRequests
                        .AnyAsync(r => r.OrderItemId == refund.OrderItemId &&
                                      r.Status == "Approved" &&
                                      r.Id != refund.Id);

                    if (existingApprovedRefund)
                    {
                        return BadRequest("This order item has already been refunded");
                    }
                }

                if (processDto.Status != "Approved" && processDto.Status != "Rejected")
                {
                    return BadRequest("Status must be either 'Approved' or 'Rejected'");
                }

                refund.Status = processDto.Status;
                refund.AdminNotes = processDto.AdminNotes;
                refund.ProcessedDate = DateTime.UtcNow;

                if (processDto.Status == "Approved")
                {
                    // Calculate refund amount
                    decimal refundAmount = refund.OrderItem.PriceAtTimeOfPurchase * refund.OrderItem.Quantity;
                    refund.RefundAmount = refundAmount;

                    // 1. Refund to customer's wallet balance
                    refund.Customer.WalletBalance += refundAmount;

                    // 2. Restore product stock
                    var productVariant = await _context.ProductVariants
                        .FindAsync(refund.OrderItem.ProductVariantId);

                    if (productVariant != null)
                    {
                        productVariant.StockQuantity += refund.OrderItem.Quantity;
                    }

                    // 3. Mark order item as refunded
                    refund.OrderItem.IsRefunded = true;
                    refund.OrderItem.RefundRequestId = refund.Id;

                    // 4. Update order status
                    await UpdateOrderStatusBasedOnRefunds(refund.OrderId);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    message = $"Refund request has been {processDto.Status.ToLower()}",
                    refundAmount = processDto.Status == "Approved" ? refund.RefundAmount : 0
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
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
        private async Task UpdateOrderStatusBasedOnRefunds(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order != null)
            {
                var totalItems = order.OrderItems.Count;
                var refundedItems = order.OrderItems.Count(oi => oi.IsRefunded);

                if (totalItems == refundedItems && totalItems > 0)
                {
                    order.Status = "Fully Refunded";
                    order.PaymentStatus = "Refunded";
                }
                else if (refundedItems > 0)
                {
                    order.Status = "Partially Refunded";
                    order.PaymentStatus = "Partially Refunded";
                }
                // If no items refunded, status remains as is

                await _context.SaveChangesAsync();
            }
        }

        private async Task UpdateBrandRefundStatistics(string brandId)
        {
            var brandStats = await _context.BrandStatistics
                .FirstOrDefaultAsync(bs => bs.BrandId == brandId);

            if (brandStats == null)
            {
                brandStats = new BrandStatistics
                {
                    BrandId = brandId,
                    TotalRefunds = 1,
                    TotalRefundAmount = 0,
                    LastUpdated = DateTime.UtcNow
                };
                _context.BrandStatistics.Add(brandStats);
            }
            else
            {
                brandStats.TotalRefunds += 1;
                brandStats.LastUpdated = DateTime.UtcNow;
            }
        }

       

    }
}