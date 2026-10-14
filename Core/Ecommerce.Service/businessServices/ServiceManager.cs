using AutoMapper;
using Ecommerce.Abstraction.Services;
using Ecommerce.Domain.Models.Contracts.RedisInMemoryRepository;
using Ecommerce.Domain.Models.Contracts.UOW;
using Ecommerce.Domain.Models.Identity;
using Ecommerce.Service.businessServices.CartServices;
using Ecommerce.Service.businessServices.OrderServices;
using Ecommerce.Service.businessServices.PaymentService;
using Ecommerce.Service.businessServices.ProductServices;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace Ecommerce.Service.businessServices
{
    /// <summary>
    /// Central service manager that lazily initialises all application services.
    /// Pattern: Controller ? ServiceManager ? Business Service ? UnitOfWork ? Repository ? Database
    /// </summary>
    public class ServiceManager(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICartRepo cartRepo,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration) : IServiceManager
    {
        private readonly Lazy<IProductService> _lazyProductService =
            new(() => new ProductService(unitOfWork, mapper));

        private readonly Lazy<ICartService> _lazyCartService =
            new(() => new CartService(cartRepo, mapper));

        private readonly Lazy<IAuthenticationService> _lazyAuthService =
            new(() => new AuthenticationService(userManager, configuration, mapper));

        private readonly Lazy<IOrderService> _lazyOrderService =
            new(() => new OrderService(mapper, cartRepo, unitOfWork));

        private readonly Lazy<IPaymentService> _lazyPaymentService =
            new(() => new PaymentService.PaymentService(configuration, cartRepo, unitOfWork, mapper));

        public IProductService ProductServices        => _lazyProductService.Value;
        public ICartService    CartService            => _lazyCartService.Value;
        public IAuthenticationService AuthenticationService => _lazyAuthService.Value;
        public IOrderService   OrderService           => _lazyOrderService.Value;
        public IPaymentService PaymentService         => _lazyPaymentService.Value;
    }
}
