using AutoMapper;
using Ecommerce.Abstraction.Services;
using Ecommerce.Domain.Exceptions;
using Ecommerce.Domain.Models.Contracts.UOW;
using Ecommerce.Domain.Models.Products;
using Ecommerce.Service.Specifications;
using Ecommerce.Shared.Common.Pagination_Result;
using Ecommerce.Shared.Common.Specification_Pattern_Enhancment;
using Ecommerce.Shared.DTOs.ProductDro_s;

namespace Ecommerce.Service.businessServices.ProductServices
{
    public class ProductService(IUnitOfWork unitOfWork, IMapper mapper) : IProductService
    {
        public async Task<PaginationResult<ProductDto>> GetProductsAsync(ProductQueryPrams query)
        {
            var repo     = unitOfWork.GetRepository<Product, int>();
            var spec     = new ProductSpecifications(query);
            var products = await repo.GetAllWithSpecificatonsAsync(spec);
            var data     = mapper.Map<IEnumerable<Product>, IEnumerable<ProductDto>>(products);

            var countSpec = new ProductCountSpecification(query);
            var totalCount = await repo.GetCountWithSpecificatonsAsync(countSpec);

            return new PaginationResult<ProductDto>(query.PageIndex, data.Count(), totalCount, data);
        }

        public async Task<ProductDto> GetProductByIdAsync(int id)
        {
            var spec    = new ProductSpecifications(id);
            var product = await unitOfWork.GetRepository<Product, int>().GetByIdWithSpecificationsAync(spec)
                ?? throw new ProductNotFound(id);

            return mapper.Map<Product, ProductDto>(product);
        }

        public async Task<IEnumerable<BrandDto>> GetBrandsAsync()
        {
            var brands = await unitOfWork.GetRepository<ProductBrand, int>().GetAllAsync();
            return mapper.Map<IEnumerable<ProductBrand>, IEnumerable<BrandDto>>(brands);
        }

        public async Task<IEnumerable<TypeDto>> GetTypesAsync()
        {
            var types = await unitOfWork.GetRepository<ProductType, int>().GetAllAsync();
            return mapper.Map<IEnumerable<ProductType>, IEnumerable<TypeDto>>(types);
        }
    }
}
