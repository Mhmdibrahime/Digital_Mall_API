using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.DTOs.SuperAdminDTOs.OrdersManagementDTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Digital_Mall_API.Controllers.BrandAdmin
{
    [Route("Brand/Management/[controller]")]
    [ApiController]
    public class BrandOrdersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BrandOrdersController(AppDbContext context)
        {
            _context = context;
        }


        [HttpGet("All")]
        public async Task<IActionResult> GetOrders(
    [FromQuery] string? search,
    [FromQuery] string? paymentStatus,
    [FromQuery] string? shippingStatus,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 30)
        {
            var brandId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(brandId))
                return Unauthorized("Brand identifier not found in token");

            var query = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Brand)
                .AsQueryable();

            
            query = query.Where(o => o.OrderItems.Any(oi => oi.BrandId == brandId));

           
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(o =>
                    o.Id.ToString().Contains(search) ||
                    o.Customer.FullName.Contains(search));
            }

           
            if (!string.IsNullOrEmpty(paymentStatus) && paymentStatus != "All")
            {
                query = query.Where(o => o.PaymentStatus == paymentStatus);
            }

          
            if (!string.IsNullOrEmpty(shippingStatus) && shippingStatus != "All")
            {
                query = query.Where(o => o.Status == shippingStatus);
            }

            var totalCount = await query.CountAsync();

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new
                {
                    o,
                    BrandItems = o.OrderItems.Where(oi => oi.BrandId == brandId)
                })
                .Select(x => new OrderDto
                {
                    Id = x.o.Id,
                    OrderNumber = $"ORD-{x.o.Id:D3}",
                    CustomerName = x.o.Customer.FullName,
                    CustomerEmail = x.o.Customer.Email,

                  
                    BrandNames = x.o.OrderItems
                        .Select(oi => oi.Brand.OfficialName)
                        .Distinct()
                        .ToList(),

                    OrderDate = x.o.OrderDate,
                    TotalAmount = x.BrandItems.Sum(oi => oi.PriceAtTimeOfPurchase * oi.Quantity),
                    ItemsCount = x.BrandItems.Sum(oi => oi.Quantity),
                    PaymentStatus = x.o.PaymentStatus,
                    PaymentMethod = x.o.PaymentMethod_Type,
                    ShippingStatus = x.o.Status,
                    ShippingTrackingNumber = x.o.ShippingTrackingNumber
                })
                .ToListAsync();

            return Ok(new
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Orders = orders
            });
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderDetails(int id)
        {
            var brandId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(brandId))
                return Unauthorized("Brand identifier not found in token");

            
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Brand)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.ProductVariant)
                    .ThenInclude(pv => pv.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.OrderItems.Any(oi => oi.BrandId == brandId));

            if (order == null)
                return NotFound();

           
            var brandOrderItems = order.OrderItems
                .Where(oi => oi.BrandId == brandId)
                .ToList();

            var orderDetails = new OrderDetailDto
            {
                Id = order.Id,
                OrderNumber = $"ORD-{order.Id:D3}",
                CustomerName = order.Customer.FullName,
                CustomerEmail = order.Customer.Email,

                
                BrandNames = brandOrderItems
                    .Select(oi => oi.Brand.OfficialName)
                    .Distinct()
                    .ToList(),

                OrderDate = order.OrderDate,
                TotalAmount = brandOrderItems.Sum(oi => oi.Quantity * oi.PriceAtTimeOfPurchase),
                PaymentStatus = order.PaymentStatus,
                PaymentMethod = order.PaymentMethod_Type,
                ShippingStatus = order.Status,
                ShippingTrackingNumber = order.ShippingTrackingNumber,
                EstimatedDelivery = order.OrderDate.AddDays(5),
                ShippingAddress = $"{order.ShippingAddress_Building}, {order.ShippingAddress_Street}, {order.ShippingAddress_City}, {order.ShippingAddress_Country}",

                OrderItems = brandOrderItems.Select(oi => new OrderItemDto
                {
                    BrandName = oi.Brand.OfficialName,
                    ProductName = oi.ProductVariant.Product.Name,
                    Quantity = oi.Quantity,
                    Size = oi.ProductVariant.Size,
                    Color = oi.ProductVariant.Color,
                    Price = oi.PriceAtTimeOfPurchase,
                    Total = oi.Quantity * oi.PriceAtTimeOfPurchase
                }).ToList(),

                Notes = order.Notes
            };

            return Ok(orderDetails);
        }



        [HttpGet("PaymentStatusOptions")]
        public IActionResult GetPaymentStatusOptions()
        {
            var options = new[]
            {
                "All",
                "Paid",
                "Pending",
                "COD"
            };

            return Ok(options);
        }

       
        [HttpGet("ShippingStatusOptions")]
        public IActionResult GetShippingStatusOptions()
        {
            var options = new[]
            {
                "All",
                "Pending",
                "Processing",
                "Shipped",
                "Delivered",
                "Cancelled"
            };

            return Ok(options);
        }
    }


}
