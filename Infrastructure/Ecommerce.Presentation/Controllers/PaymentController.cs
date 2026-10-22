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
                // Bắt buộc xác thực chữ ký nếu webhookSecret được thiết lập trong cấu hình
                if (!string.IsNullOrEmpty(webhookSecret))
                {
                    stripeEvent = Stripe.EventUtility.ConstructEvent(json, stripeSignature, webhookSecret);
                }
                else
                {
                    // Chế độ dev / fallback kiểm thử
                    stripeEvent = Stripe.EventUtility.ParseEvent(json);
                }
            }
            catch (Exception)
            {
                // Chữ ký không hợp lệ -> từ chối ngay lập tức
                return BadRequest("Invalid Stripe signature or payload.");
            }

            var db = redis.GetDatabase();

            // 1. Chống trùng lặp Event (Event Idempotency):
            // Lưu event.Id trong 24 giờ. Nếu Stripe gửi lại cùng một event, lập tức trả về Ok() mà không xử lý lại
            var eventKey = $"webhook:event:{stripeEvent.Id}";
            var isNewEvent = await db.StringSetAsync(eventKey, "processed", TimeSpan.FromHours(24), StackExchange.Redis.When.NotExists);
            if (!isNewEvent)
            {
                return Ok();
            }

            if (stripeEvent.Data.Object is Stripe.PaymentIntent intent)
            {
                var lockKey = $"lock:payment:{intent.Id}";

                // 2. Distributed Lock qua Redis: tránh race condition giữa Webhook và Background Job
                var acquired = await db.StringSetAsync(lockKey, "locked", TimeSpan.FromSeconds(30), StackExchange.Redis.When.NotExists);
                if (!acquired)
                {
                    // Nếu đang có tiến trình xử lý intent này, hoàn tác eventKey để Stripe có thể retry hoặc trả Ok
                    await db.KeyDeleteAsync(eventKey);
                    return StatusCode(429); // Đang xử lý bận, để Stripe retry sau
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
