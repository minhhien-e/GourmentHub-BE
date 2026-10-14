using AutoMapper;
using Ecommerce.Abstraction.Services;
using Ecommerce.Domain.Exceptions;
using Ecommerce.Domain.Models.Contracts.RedisInMemoryRepository;
using Ecommerce.Domain.Models.Contracts.UOW;
using Ecommerce.Domain.Models.Orders;
using Ecommerce.Domain.Models.Products;
using Ecommerce.Service.Specifications;
using Ecommerce.Shared.DTOs.IdentityDto_s;
using Ecommerce.Shared.DTOs.OrderDto_s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ecommerce.Service.businessServices.OrderSevices
{
    public class OrderSevice(IMapper mapper, ICartRepo cartRepo, IUnitOfWork unitOfWork) : IOrderService
    {
        public async Task<OrderToReturnDto> CreateOrderAsync(OrderDto orderDto, string email)
        {
            var orderAddress = mapper.Map<AddressDto, OrderAddress>(orderDto.Address);
            var cart = await cartRepo.GetCartAsync(orderDto.CartId) ?? throw new CartNotFound(orderDto.CartId);

            ArgumentNullException.ThrowIfNullOrEmpty(cart.PaymentIntentId);

            var orderRepo = unitOfWork.GetRepository<Order, Guid>();
            var spec = new OrderIntentCheck(cart.PaymentIntentId);
            var existingOrder = await orderRepo.GetByIdWithSpecificationsAync(spec);

            if (existingOrder is not null)
            {
                orderRepo.delete(existingOrder);
            }

            var orderItems = new List<OrderItem>();
            var productRepo = unitOfWork.GetRepository<Product, int>();

            foreach (var item in cart.Items)
            {
                var product = await productRepo.GetByIdAsync(item.Id) ?? throw new ProductNotFound(item.Id);
                var orderItem = new OrderItem
                {
                    Product = new ItemTobeOrderd
                    {
                        ProductId = item.Id,
                        ProductImgUrl = product.PictureUrl,
                        ProductName = item.Name,
                    },
                    Quantity = item.Quantity,
                    Price = product.Price
                };
                orderItems.Add(orderItem);
            }

            var deliveryMethod = await unitOfWork.GetRepository<DeliveryMethod, int>().GetByIdAsync(orderDto.DeliveryMethodId) 
                ?? throw new DeliveryMethodNotFound(orderDto.DeliveryMethodId);

            var subTotal = orderItems.Sum(p => p.Price * p.Quantity);
            var order = new Order(email, orderAddress, deliveryMethod, orderItems, subTotal, cart.PaymentIntentId);

            unitOfWork.GetRepository<Order, Guid>().add(order);
            await unitOfWork.SaveChangesAsync();

            return mapper.Map<Order, OrderToReturnDto>(order);
        }

        public async Task<IEnumerable<OrderToReturnDto>> GetAllOrdersAsync(string email)
        {
            var spec = new OrderSpecifications(email);
            var orders = await unitOfWork.GetRepository<Order, Guid>().GetAllWithSpecificatonsAsync(spec);
            return mapper.Map<IEnumerable<Order>, IEnumerable<OrderToReturnDto>>(orders);
        }

        public async Task<OrderToReturnDto> GetOrderById(Guid orderId)
        {
            var spec = new OrderSpecifications(orderId);
            var order = await unitOfWork.GetRepository<Order, Guid>().GetByIdWithSpecificationsAync(spec);
            return mapper.Map<Order, OrderToReturnDto>(order);
        }

        public async Task<IEnumerable<DeliveryMethodDto>> GetDeliveryMethodsAsync()
        {
            var deliveryMethods = await unitOfWork.GetRepository<DeliveryMethod, int>().GetAllAsync();
            return mapper.Map<IEnumerable<DeliveryMethod>, IEnumerable<DeliveryMethodDto>>(deliveryMethods);
        }
    }
}
