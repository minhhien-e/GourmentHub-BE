using Ecommerce.Abstraction.Services;
using Ecommerce.Presentation.Attributes;
using Ecommerce.Shared.DTOs.CartDto_s;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Ecommerce.Presentation.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class CartController(IServiceManger manager) : ControllerBase
    {
        [HttpGet]
        [Cache(100000)]
        public async Task<ActionResult<CartDto>> GetCart([FromQuery] string key)
        {
            var cart = await manager.CartService.GetCartAsync(key);
            return Ok(cart);
        }

        [HttpPost]
        public async Task<ActionResult<CartDto>> CreateOrUpdateCart([FromBody] CartDto cart)
        {
            var savedCart = await manager.CartService.CreateUpdateCartAsync(cart);
            return Ok(savedCart);
        }

        [HttpDelete("{key}")]
        public async Task<ActionResult<bool>> DeleteCart(string key)
        {
            var result = await manager.CartService.DeleteCartAsync(key);
            return Ok(result);
        }
    }
}
