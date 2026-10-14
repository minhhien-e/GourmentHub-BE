using Ecommerce.Abstraction.Services;
using Ecommerce.Presentation.Attributes;
using Ecommerce.Shared.Common.Pagination_Result;
using Ecommerce.Shared.Common.Specification_Pattern_Enhancment;
using Ecommerce.Shared.DTOs.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ecommerce.Presentation.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class ProductController(IServiceManager manager) : ControllerBase
    {
        [HttpGet]
        [Cache(4200)]
        public async Task<ActionResult<PaginationResult<ProductDto>>> GetAllProducts([FromQuery] ProductQueryPrams productQueryParams)
        {
            var products = await manager.ProductServices.GetProductsAsync(productQueryParams);
            return Ok(products);
        }

        [Cache(1000)]
        [HttpGet("brands")]
        public async Task<ActionResult<IEnumerable<BrandDto>>> GetAllProductBrands()
        {
            var brands = await manager.ProductServices.GetBrandsAsync();
            return Ok(brands);
        }

        [Cache(1000)]
        [HttpGet("types")]
        public async Task<ActionResult<IEnumerable<TypeDto>>> GetAllProductTypes()
        {
            var types = await manager.ProductServices.GetTypesAsync();
            return Ok(types);
        }

        [Cache(1000)]
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDto>> GetProductById(int id)
        {
            var product = await manager.ProductServices.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return Ok(product);
        }
    }
}
