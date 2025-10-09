using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.DTOs.SuperAdminDTOs.OrdersManagementDTOs;
using Digital_Mall_API.Models.Entities.Orders___Shopping;
using Digital_Mall_API.Models.Entities.Product_Catalog;
using Digital_Mall_API.Models.Entities.User___Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Digital_Mall_API.Controllers.SuperAdmin
{
    [Route("Super/[controller]")]
    [ApiController]
    public class OrdersManagementController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OrdersManagementController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetOrders(
     [FromQuery] string? search,
     [FromQuery] string? paymentStatus,
     [FromQuery] string? shippingStatus,
     [FromQuery] int page = 1,
     [FromQuery] int pageSize = 30)
        {
            var query = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Brand)
                .AsQueryable();

            // 🔍 البحث
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(o =>
                    o.Id.ToString().Contains(search) ||
                    o.Customer.FullName.Contains(search) ||
                    o.OrderItems.Any(oi => oi.Brand.OfficialName.Contains(search)));
            }

            // 💰 فلترة حسب حالة الدفع
            if (!string.IsNullOrEmpty(paymentStatus) && paymentStatus != "All")
            {
                query = query.Where(o => o.PaymentStatus == paymentStatus);
            }

            // 🚚 فلترة حسب حالة الشحن
            if (!string.IsNullOrEmpty(shippingStatus) && shippingStatus != "All")
            {
                query = query.Where(o => o.Status == shippingStatus);
            }

            var totalCount = await query.CountAsync();
            query = query.OrderByDescending(o => o.OrderDate);

            // 📦 تحميل البيانات مع البراندات
            var orders = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new OrderDto
                {
                    Id = o.Id,
                    OrderNumber = $"ORD-{o.Id:D3}",
                    CustomerName = o.Customer.FullName,
                    CustomerEmail = o.Customer.Email,

                    // 🏷️ بدل BrandName واحد، بنعرض أسماء البراندز المشتركة في الأوردر
                    BrandNames = o.OrderItems
                        .Select(oi => oi.Brand.OfficialName)
                        .Distinct()
                        .ToList(),

                    OrderDate = o.OrderDate,
                    TotalAmount = o.OrderItems.Sum(oi => oi.Quantity * oi.PriceAtTimeOfPurchase),
                    ItemsCount = o.OrderItems.Sum(oi => oi.Quantity),
                    PaymentStatus = o.PaymentStatus,
                    PaymentMethod = o.PaymentMethod_Type,
                    ShippingStatus = o.Status,
                    ShippingTrackingNumber = o.ShippingTrackingNumber
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
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Brand)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            var orderDetails = new OrderDetailDto
            {
                Id = order.Id,
                OrderNumber = $"ORD-{order.Id:D3}",
                CustomerName = order.Customer.FullName,
                CustomerEmail = order.Customer.Email,

                // ✅ عرض كل البراندات المشاركة في الأوردر
                BrandNames = order.OrderItems
                    .Select(oi => oi.Brand.OfficialName)
                    .Distinct()
                    .ToList(),

                OrderDate = order.OrderDate,
                TotalAmount = order.OrderItems.Sum(oi => oi.Quantity * oi.PriceAtTimeOfPurchase),
                PaymentStatus = order.PaymentStatus,
                PaymentMethod = order.PaymentMethod_Type,
                ShippingStatus = order.Status,
                ShippingTrackingNumber = order.ShippingTrackingNumber,
                EstimatedDelivery = order.OrderDate.AddDays(5),
                ShippingAddress = $"{order.ShippingAddress_Building}, {order.ShippingAddress_Street}, {order.ShippingAddress_City}, {order.ShippingAddress_Country}",

                // ✅ تفاصيل كل منتج في الأوردر
                OrderItems = order.OrderItems.Select(oi => new OrderItemDto
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


        [HttpPut("{id}/PaymentStatus")]
        public async Task<IActionResult> UpdatePaymentStatus(int id, [FromBody] UpdatePaymentStatusRequest request)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            order.PaymentStatus = request.PaymentStatus;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Payment status updated successfully" });
        }

        [HttpPut("{id}/ShippingStatus")]
        public async Task<IActionResult> UpdateShippingStatus(int id, [FromBody] UpdateShippingStatusRequest request)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            order.Status = request.Status;


            await _context.SaveChangesAsync();

            return Ok(new { Message = "Shipping status updated successfully" });
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