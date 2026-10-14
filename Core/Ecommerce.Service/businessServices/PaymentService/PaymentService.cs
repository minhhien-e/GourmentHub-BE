using AutoMapper;
using Ecommerce.Abstraction.Services;
using Ecommerce.Domain.Exceptions;
using Ecommerce.Domain.Models.Cart;
using Ecommerce.Domain.Models.Contracts.RedisInMemoryRepository;
using Ecommerce.Domain.Models.Contracts.UOW;
using Ecommerce.Domain.Models.Orders;
using Ecommerce.Shared.DTOs.Cart;
using Microsoft.Extensions.Configuration;
using Stripe;
using Product = Ecommerce.Domain.Models.Products.Product;

namespace Ecommerce.Service.businessServices.PaymentService
{
    public class PaymentService(
        IConfiguration configuration,
        ICartRepo cartRepo,
        IUnitOfWork unitOfWork,
        IMapper mapper) : IPaymentService
    {
        public async Task<CartDto> CreateOrUpdatePaymentIntentAsync(string cartId)
        {
            StripeConfiguration.ApiKey = configuration["StripeSettings:secretKey"];

            var cart = await cartRepo.GetCartAsync(cartId)
                ?? throw new CartNotFound(cartId);

            // Refresh item prices from the database to prevent client-side tampering
            var productRepo = unitOfWork.GetRepository<Product, int>();
            foreach (var item in cart.Items)
            {
                var product = await productRepo.GetByIdAsync(item.Id)
                    ?? throw new ProductNotFound(item.Id);
                item.Price = product.Price;
            }

            var deliveryMethod = await unitOfWork.GetRepository<DeliveryMethod, int>()
                .GetByIdAsync(cart.DeliveryMethodId!.Value)
                ?? throw new DeliveryMethodNotFound(cart.DeliveryMethodId.Value);

            cart.ShippingPrice = deliveryMethod.Price;

            var amountInCents = (long)(cart.Items.Sum(i => i.Quantity * i.Price) + deliveryMethod.Price) * 100;

            var intentService = new PaymentIntentService();

            if (cart.PaymentIntentId is null)
            {
                var createOptions = new PaymentIntentCreateOptions
                {
                    Amount             = amountInCents,
                    Currency           = "usd",
                    PaymentMethodTypes = ["card"]
                };
                var intent         = await intentService.CreateAsync(createOptions);
                cart.PaymentIntentId = intent.Id;
                cart.ClientSecret    = intent.ClientSecret;
            }
            else
            {
                var updateOptions = new PaymentIntentUpdateOptions { Amount = amountInCents };
                await intentService.UpdateAsync(cart.PaymentIntentId, updateOptions);
            }

            await cartRepo.CreateUpdateCartAsync(cart);
            return mapper.Map<UserCart, CartDto>(cart);
        }
    }
}
