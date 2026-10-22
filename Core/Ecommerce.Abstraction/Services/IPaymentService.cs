using Ecommerce.Shared.DTOs.Cart;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Abstraction.Services
{
    public interface IPaymentService
    {
        Task<CartDto> CreateOrUpdatePaymentIntentAsync(string CartId);
        Task<bool> UpdatePaymentStatusAsync(string paymentIntentId, bool isSuccess);
    }
}
