using AutoMapper;
using Ecommerce.Domain.Models.Contracts.Repository.GenericReposatory;
using Ecommerce.Domain.Models.Contracts.Specifications;
using Ecommerce.Domain.Models.Contracts.UOW;
using Ecommerce.Domain.Models.Products;
using Ecommerce.Domain.Exceptions;
using Ecommerce.Service.businessServices.ProductServices;
using Ecommerce.Shared.DTOs.ProductDro_s;
using FluentAssertions;
using Moq;

namespace GourmetHub.Tests.Services
{
    public class ProductServiceTests
    {
        private readonly Mock<IUnitOfWork>                _unitOfWorkMock = new();
        private readonly Mock<IMapper>                    _mapperMock     = new();
        private readonly Mock<IGenericRepo<Product, int>> _repoMock       = new();
        private readonly ProductService                   _sut;

        public ProductServiceTests()
        {
            _unitOfWorkMock.Setup(u => u.GetRepository<Product, int>()).Returns(_repoMock.Object);
            _sut = new ProductService(_unitOfWorkMock.Object, _mapperMock.Object);
        }

        [Fact]
        public async Task GetProductByIdAsync_WhenProductNotFound_ThrowsProductNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdWithSpecificationsAync(
                It.IsAny<ISpecification<Product, int>>()))
                .ReturnsAsync((Product)null!);

            var act = async () => await _sut.GetProductByIdAsync(999);

            await act.Should().ThrowAsync<ProductNotFound>();
        }

        [Fact]
        public async Task GetProductByIdAsync_WhenProductExists_ReturnsMappedDto()
        {
            var product    = new Product    { Id = 1, Name = "Spaghetti Carbonara", Price = 9.99m };
            var productDto = new ProductDto { id = 1, Name = "Spaghetti Carbonara" };

            _repoMock  .Setup(r => r.GetByIdWithSpecificationsAync(It.IsAny<ISpecification<Product, int>>()))
                       .ReturnsAsync(product);
            _mapperMock.Setup(m => m.Map<Product, ProductDto>(product)).Returns(productDto);

            var result = await _sut.GetProductByIdAsync(1);

            result.Should().NotBeNull();
            result.Name.Should().Be("Spaghetti Carbonara");
        }

        [Fact]
        public async Task GetBrandsAsync_ReturnsAllBrands()
        {
            var brandRepo = new Mock<IGenericRepo<ProductBrand, int>>();
            var brands    = new List<ProductBrand> { new() { Id = 1, Name = "Italian" }, new() { Id = 2, Name = "Asian" } };
            var brandDtos = new List<BrandDto>     { new() { Id = 1, Name = "Italian" }, new() { Id = 2, Name = "Asian" } };

            brandRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(brands);
            _unitOfWorkMock.Setup(u => u.GetRepository<ProductBrand, int>()).Returns(brandRepo.Object);
            _mapperMock    .Setup(m => m.Map<IEnumerable<ProductBrand>, IEnumerable<BrandDto>>(brands)).Returns(brandDtos);

            var result = await _sut.GetBrandsAsync();

            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetTypesAsync_ReturnsAllTypes()
        {
            var typeRepo = new Mock<IGenericRepo<ProductType, int>>();
            var types    = new List<ProductType> { new() { Id = 1, Name = "Pasta" } };
            var typeDtos = new List<TypeDto>     { new() { Id = 1, Name = "Pasta" } };

            typeRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(types);
            _unitOfWorkMock.Setup(u => u.GetRepository<ProductType, int>()).Returns(typeRepo.Object);
            _mapperMock    .Setup(m => m.Map<IEnumerable<ProductType>, IEnumerable<TypeDto>>(types)).Returns(typeDtos);

            var result = await _sut.GetTypesAsync();

            result.Should().HaveCount(1);
        }
    }
}
