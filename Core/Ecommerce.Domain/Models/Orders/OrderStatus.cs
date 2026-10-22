using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Domain.Models.Orders
{
    public enum OrderStatus
    {
        pending = 0,
        PaymentReceived = 1,
        PaymentFailed = 2,
        Shipped = 3,
        Deliverd = 4,
        Cancelled = 5,
    }
}
