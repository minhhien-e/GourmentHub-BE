using Ecommerce.Abstraction.Services;
using Ecommerce.Presentation.Attributes;
using Ecommerce.Shared.DTOs.OrderDto_s;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ecommerce.Presentation.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class OrderController(IServiceManger manager) : ControllerBase
    {
        [HttpPost]
        public async Task<ActionResult<OrderToReturnDto>> CreateOrder([FromBody] OrderDto orderDto)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var order = await manager.OrderService.CreateOrderAsync(orderDto, email);
            return Ok(order);
        }

        [HttpGet("delivery-methods")]
        public async Task<ActionResult<IEnumerable<DeliveryMethodDto>>> GetDeliveryMethods()
        {
            var deliveryMethods = await manager.OrderService.GetDeliveryMethodsAsync();
            return Ok(deliveryMethods);
        }

        [HttpGet("my-orders")]
        [Cache(4200)]
        public async Task<ActionResult<IEnumerable<OrderToReturnDto>>> GetAllOrders()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var orders = await manager.OrderService.GetAllOrdersAsync(email);
            return Ok(orders);
        }

        [HttpGet("{id:guid}")]
        [Cache(1000)]
        public async Task<ActionResult<OrderToReturnDto>> GetOrderById(Guid id)
        {
            var order = await manager.OrderService.GetOrderById(id);
            return Ok(order);
        }
    }
}
