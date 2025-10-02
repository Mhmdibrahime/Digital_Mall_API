using Digital_Mall_API.Models.Data;
using Digital_Mall_API.Models.DTOs.DesignerAdminDTOs;
using Digital_Mall_API.Models.Entities.T_Shirt_Customization;
using Digital_Mall_API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Digital_Mall_API.Controllers.DesignerAdmin
{
    [Route("Designer/[controller]")]
    [ApiController]
    public class RequestController : ControllerBase
    {
        private readonly AppDbContext context;

        public RequestController(AppDbContext context)
        {
            this.context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TshirtDesignOrderListDto>>> GetAll(
     [FromQuery] int? requestId,
     [FromQuery] string status = "All",
     [FromQuery] int pageNumber = 1,
     [FromQuery] int pageSize = 30)
        {
            var query = context.TshirtDesignOrders
                .Include(o => o.CustomerUser)
                .AsQueryable();


            if (requestId.HasValue)
            {
                query = query.Where(o => o.Id == requestId.Value);
            }


            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                query = query.Where(o => o.Status == status);
            }


            var totalCount = await query.CountAsync();


            var orders = await query
                .OrderByDescending(o => o.RequestDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = orders.Select(o => new TshirtDesignOrderListDto
            {
                RequestId = o.Id,
                Customer = o.CustomerUser != null ? o.CustomerUser.FullName : "Unknown",
                RequestDate = o.RequestDate,
                Status = o.Status
            });

            return Ok(new
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Data = result
            });
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<TshirtDesignOrderDto>> GetById(int id)
        {
            var order = await context.TshirtDesignOrders
                .Include(o => o.CustomerUser)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            return new TshirtDesignOrderDto
            {
                Id = order.Id,
                CustomerName = order.CustomerUser.FullName,
                CustomerEmail = order.CustomerUser.Email,
                ChosenColor = order.ChosenColor,
                ChosenStyle = order.ChosenStyle,
                ChosenSize = order.ChosenSize,
                CustomerDescription = order.CustomerDescription,
                CustomerImageUrl = order.CustomerImageUrl,
                FinalDesignUrl = order.FinalDesignUrl,
                Status = order.Status,
                RequestDate = order.RequestDate
            };
        }


        [HttpPost]
        public async Task<ActionResult<TshirtDesignOrderDto>> Create(TshirtDesignOrderCreateDto dto)
        {
            var order = new TshirtDesignOrder
            {
                CustomerUserId = dto.CustomerUserId,
                ChosenColor = dto.ChosenColor,
                ChosenStyle = dto.ChosenStyle,
                ChosenSize = dto.ChosenSize,
                CustomerDescription = dto.CustomerDescription,
                CustomerImageUrl = dto.CustomerImageUrl,
                Status = "Pending",
                FinalPrice = 0,
                IsPaid = false,
                RequestDate = DateTime.UtcNow
            };

            context.TshirtDesignOrders.Add(order);
            await context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, TshirtDesignOrderUpdateDto dto)
        {
            var order = await context.TshirtDesignOrders.FindAsync(id);
            if (order == null) return NotFound();

            order.ChosenColor = dto.ChosenColor;
            order.ChosenStyle = dto.ChosenStyle;
            order.ChosenSize = dto.ChosenSize;
            order.CustomerDescription = dto.CustomerDescription;
            order.CustomerImageUrl = dto.CustomerImageUrl;
            order.Status = dto.Status;


            await context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var order = await context.TshirtDesignOrders.FindAsync(id);
            if (order == null) return NotFound();

            context.TshirtDesignOrders.Remove(order);
            await context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ChangeStatus(int id, [FromBody] TshirtDesignOrderStatusDto dto)
        {
            var order = await context.TshirtDesignOrders.FindAsync(id);
            if (order == null) return NotFound();

            order.Status = dto.Status;
            await context.SaveChangesAsync();

            return Ok(new
            {
                Message = $"Order {id} status updated successfully.",
                NewStatus = order.Status
            });
        }
    }
}

