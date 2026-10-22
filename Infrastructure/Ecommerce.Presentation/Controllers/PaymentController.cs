using Ecommerce.Abstraction.Services;
using Ecommerce.Shared.DTOs.Cart;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Ecommerce.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController(IServiceManager manager) : ControllerBase
    {
        [Authorize]
        [HttpPost("{cartId}")]
        public async Task<ActionResult<CartDto>> CreateOrUpdatePaymentIntent(string cartId)
        {
            var cart = await manager.PaymentService.CreateOrUpdatePaymentIntentAsync(cartId);
            return Ok(cart);
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> StripeWebhook([FromServices] Microsoft.Extensions.Configuration.IConfiguration config, [FromServices] StackExchange.Redis.IConnectionMultiplexer redis)
        {
            var json = await new System.IO.StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var stripeSignature = Request.Headers["Stripe-Signature"];
            var webhookSecret = config["StripeSettings:WebhookSecret"];

            Stripe.Event stripeEvent;
            try
            {
                if (!string.IsNullOrEmpty(webhookSecret))
                {
                    stripeEvent = Stripe.EventUtility.ConstructEvent(json, stripeSignature, webhookSecret);
                }
                else
                {
                    stripeEvent = Stripe.EventUtility.ParseEvent(json);
                }
            }
            catch (Exception)
            {
                return BadRequest();
            }

            if (stripeEvent.Data.Object is Stripe.PaymentIntent intent)
            {
                var lockKey = $"lock:payment:{intent.Id}";
                var db = redis.GetDatabase();

                // Distributed Lock qua Redis để tránh trùng lặp giữa Webhook và Background Job
                var acquired = await db.StringSetAsync(lockKey, "locked", TimeSpan.FromSeconds(30), StackExchange.Redis.When.NotExists);
                if (!acquired)
                {
                    // Đang có tiến trình khác xử lý intent này, trả về Ok để Stripe không retry dồn dập
                    return Ok();
                }

                try
                {
                    switch (stripeEvent.Type)
                    {
                        case Stripe.Events.PaymentIntentSucceeded:
                            await manager.PaymentService.UpdatePaymentStatusAsync(intent.Id, true);
                            break;
                        case Stripe.Events.PaymentIntentPaymentFailed:
                            await manager.PaymentService.UpdatePaymentStatusAsync(intent.Id, false);
                            break;
                    }
                }
                finally
                {
                    await db.KeyDeleteAsync(lockKey);
                }
            }

            return Ok();
        }
    }
}
