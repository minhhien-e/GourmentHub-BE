namespace Ecommerce.Abstraction.Services
{
    public interface IServiceManager
    {
        public IProductService ProductServices { get; }
        public ICartService CartService { get; }
        public IAuthenticationService AuthenticationService { get; }
        public IOrderService OrderService { get; }
        public IPaymentService PaymentService { get; }
    }
}
