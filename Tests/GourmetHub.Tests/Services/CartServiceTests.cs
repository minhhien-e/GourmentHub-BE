using AutoMapper;
using Ecommerce.Domain.Exceptions;
using Ecommerce.Domain.Models.Cart;
using Ecommerce.Domain.Models.Contracts.RedisInMemoryRepository;
using Ecommerce.Service.businessServices.CartServices;
using Ecommerce.Shared.DTOs.CartDto_s;
using FluentAssertions;
using Moq;

namespace GourmetHub.Tests.Services
{
    /// <summary>
    /// Tests for <see cref="CartService"/>.
    /// Note: CreateUpdateCartAsync is covered by integration tests since
    /// ICartRepo.CreateUpdateCartAsync uses optional parameters which Moq
    /// cannot intercept via expression trees.
    /// </summary>
    public class CartServiceTests
    {
        private readonly Mock<ICartRepo> _cartRepoMock = new();
        private readonly Mock<IMapper>   _mapperMock   = new();
        private readonly CartService     _sut;

        public CartServiceTests()
        {
            _sut = new CartService(_cartRepoMock.Object, _mapperMock.Object);
        }

        // ─── GetCartAsync ────────────────────────────────────────────────

        [Fact]
        public async Task GetCartAsync_WhenCartExists_ReturnsMappedCartDto()
        {
            var cartId   = "cart-123";
            var userCart = new UserCart { Id = cartId, Items = new List<CartItem>() };
            var cartDto  = new CartDto  { Id = cartId, Items = new List<CartItemDto>() };

            _cartRepoMock.Setup(r => r.GetCartAsync(cartId)).ReturnsAsync(userCart);
            _mapperMock  .Setup(m => m.Map<UserCart, CartDto>(userCart)).Returns(cartDto);

            var result = await _sut.GetCartAsync(cartId);

            result.Should().NotBeNull();
            result.Id.Should().Be(cartId);
        }

        [Fact]
        public async Task GetCartAsync_WhenCartNotFound_ThrowsCartNotFoundException()
        {
            _cartRepoMock.Setup(r => r.GetCartAsync("ghost")).ReturnsAsync((UserCart)null!);

            var act = async () => await _sut.GetCartAsync("ghost");

            await act.Should().ThrowAsync<CartNotFound>();
        }

        // ─── DeleteCartAsync ─────────────────────────────────────────────

        [Fact]
        public async Task DeleteCartAsync_WhenSuccessful_ReturnsTrue()
        {
            _cartRepoMock.Setup(r => r.DeleteCartAsync("cart-1")).ReturnsAsync(true);

            var result = await _sut.DeleteCartAsync("cart-1");

            result.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteCartAsync_WhenRepoReturnsNull_ReturnsFalse()
        {
            _cartRepoMock.Setup(r => r.DeleteCartAsync("cart-x")).ReturnsAsync((bool?)null);

            var result = await _sut.DeleteCartAsync("cart-x");

            result.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteCartAsync_VerifiesRepoCalledOnce()
        {
            _cartRepoMock.Setup(r => r.DeleteCartAsync("cart-verify")).ReturnsAsync(true);

            await _sut.DeleteCartAsync("cart-verify");

            _cartRepoMock.Verify(r => r.DeleteCartAsync("cart-verify"), Times.Once);
        }
    }
}
