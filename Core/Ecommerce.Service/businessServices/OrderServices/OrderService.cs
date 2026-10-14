using AutoMapper;
using Ecommerce.Abstraction.Services;
using Ecommerce.Domain.Exceptions;
using Ecommerce.Domain.Models.Contracts.RedisInMemoryRepository;
using Ecommerce.Domain.Models.Contracts.UOW;
using Ecommerce.Domain.Models.Orders;
using Ecommerce.Domain.Models.Products;
using Ecommerce.Service.Specifications;
using Ecommerce.Shared.DTOs.Identity;
using Ecommerce.Shared.DTOs.Orders;

namespace Ecommerce.Service.businessServices.OrderServices
{
    public class OrderService(IMapper mapper, ICartRepo cartRepo, IUnitOfWork unitOfWork) : IOrderService
    {
        public async Task<OrderToReturnDto> CreateOrderAsync(OrderDto orderDto, string email)
        {
            var orderAddress = mapper.Map<AddressDto, OrderAddress>(orderDto.Address);
            var cart         = await cartRepo.GetCartAsync(orderDto.CartId)
                ?? throw new CartNotFound(orderDto.CartId);

            ArgumentNullException.ThrowIfNullOrEmpty(cart.PaymentIntentId);

            // If an order already exists for this payment intent, remove it before creating a new one
            var orderRepo     = unitOfWork.GetRepository<Order, Guid>();
            var existingOrder = await orderRepo.GetByIdWithSpecificationsAync(new OrderIntentCheck(cart.PaymentIntentId));
            if (existingOrder is not null)
                orderRepo.delete(existingOrder);

            // Build order items from cart
            var productRepo = unitOfWork.GetRepository<Product, int>();
            var orderItems  = new List<OrderItem>();

            foreach (var item in cart.Items)
            {
                var product = await productRepo.GetByIdAsync(item.Id)
                    ?? throw new ProductNotFound(item.Id);

                orderItems.Add(new OrderItem
                {
                    Product = new ItemTobeOrderd
                    {
                        ProductId    = item.Id,
                        ProductImgUrl = product.PictureUrl,
                        ProductName  = item.Name
                    },
                    Quantity = item.Quantity,
                    Price    = product.Price
                });
            }

            var deliveryMethod = await unitOfWork.GetRepository<DeliveryMethod, int>()
                .GetByIdAsync(orderDto.DeliveryMethodId)
                ?? throw new DeliveryMethodNotFound(orderDto.DeliveryMethodId);

            var subTotal = orderItems.Sum(i => i.Price * i.Quantity);
            var order    = new Order(email, orderAddress, deliveryMethod, orderItems, subTotal, cart.PaymentIntentId);

            unitOfWork.GetRepository<Order, Guid>().add(order);
            await unitOfWork.SaveChangesAsync();

            return mapper.Map<Order, OrderToReturnDto>(order);
        }

        public async Task<IEnumerable<OrderToReturnDto>> GetAllOrdersAsync(string email)
        {
            var orders = await unitOfWork.GetRepository<Order, Guid>()
                .GetAllWithSpecificatonsAsync(new OrderSpecifications(email));

            return mapper.Map<IEnumerable<Order>, IEnumerable<OrderToReturnDto>>(orders);
        }

        public async Task<OrderToReturnDto> GetOrderById(Guid orderId)
        {
            var order = await unitOfWork.GetRepository<Order, Guid>()
                .GetByIdWithSpecificationsAync(new OrderSpecifications(orderId));

            return mapper.Map<Order, OrderToReturnDto>(order);
        }

        public async Task<IEnumerable<DeliveryMethodDto>> GetDeliveryMethodsAsync()
        {
            var deliveryMethods = await unitOfWork.GetRepository<DeliveryMethod, int>().GetAllAsync();
            return mapper.Map<IEnumerable<DeliveryMethod>, IEnumerable<DeliveryMethodDto>>(deliveryMethods);
        }
    }
}
