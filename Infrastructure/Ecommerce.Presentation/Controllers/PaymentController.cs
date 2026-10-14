using Ecommerce.Abstraction.Services;
using Ecommerce.Shared.DTOs.CartDto_s;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Ecommerce.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController(IServiceManger manager) : ControllerBase
    {
        [Authorize]
        [HttpPost("{cartId}")]
        public async Task<ActionResult<CartDto>> CreateOrUpdatePaymentIntent(string cartId)
        {
            var cart = await manager.PaymentService.CreateOrUpdatePaymentIntentAsync(cartId);
            return Ok(cart);
        }
    }
}
