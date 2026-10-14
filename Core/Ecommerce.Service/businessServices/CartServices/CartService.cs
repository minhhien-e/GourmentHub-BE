using AutoMapper;
using Ecommerce.Abstraction.Services;
using Ecommerce.Domain.Exceptions;
using Ecommerce.Domain.Models.Cart;
using Ecommerce.Domain.Models.Contracts.RedisInMemoryRepository;
using Ecommerce.Shared.DTOs.Cart;

namespace Ecommerce.Service.businessServices.CartServices
{
    public class CartService(ICartRepo cartRepo, IMapper mapper) : ICartService
    {
        public async Task<CartDto> GetCartAsync(string key)
        {
            var cart = await cartRepo.GetCartAsync(key)
                ?? throw new CartNotFound(key);

            return mapper.Map<UserCart, CartDto>(cart);
        }

        public async Task<CartDto> CreateUpdateCartAsync(CartDto cartDto)
        {
            var userCart   = mapper.Map<CartDto, UserCart>(cartDto);
            var savedCart  = await cartRepo.CreateUpdateCartAsync(userCart)
                ?? throw new Exception("Failed to save cart. Please try again.");

            return await GetCartAsync(savedCart.Id);
        }

        public async Task<bool> DeleteCartAsync(string key)
        {
            var result = await cartRepo.DeleteCartAsync(key);
            return result ?? false;
        }
    }
}
