using Ecommerce.Domain.Models.Orders;

namespace Ecommerce.Service.Specifications
{
    public class PendingOrdersSpecification : BaseSpecifications<Order, Guid>
    {
        public PendingOrdersSpecification(DateTimeOffset cutoffTime)
            : base(o => o.Status == OrderStatus.pending && o.OrderDate <= cutoffTime && !string.IsNullOrEmpty(o.PaymentIntentId))
        {
        }
    }
}
