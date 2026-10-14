using Ecommerce.Shared.DTOs.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Shared.DTOs.Orders
{
    public class OrderDto
    {
        //OrderDto[adress - delivery method - CartId] / userEmail

        public string CartId { get; set; }  

        public int DeliveryMethodId { get; set; }

        public AddressDto Address { get; set; }

    }
}
