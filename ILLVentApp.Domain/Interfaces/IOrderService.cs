using System.Collections.Generic;
using System.Threading.Tasks;
using ILLVentApp.Domain.DTOs;
using ILLVentApp.Domain.Models;

namespace ILLVentApp.Domain.Interfaces
{
    public interface IOrderService
    {
        Task<List<OrderDto>> GetUserOrdersAsync(string userId);
        Task<OrderDto> GetOrderByIdAsync(int orderId);
        Task<OrderDto> CreateOrderFromCartAsync(string userId, CheckoutDto checkoutDto);
        Task<bool> UpdateOrderStatusAsync(int orderId, string status);
        Task<bool> CancelOrderAsync(int orderId);
        Task<string> CreateStripeCheckoutSessionAsync(int orderId);
        Task<bool> HandleStripeWebhookAsync(string payload, string signature);
    }
} 