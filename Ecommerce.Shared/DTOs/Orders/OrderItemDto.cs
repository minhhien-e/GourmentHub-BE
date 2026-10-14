using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Shared.DTOs.Orders
{
    public class OrderItemDto
    {
        public string ProductName { get; set; } = null!;
        public string ProductPictureUrl { get; set; } = null!;
        public decimal price { get; set; }
        public int quantity { get; set; }
    }
}
