using System.Collections.Generic;
using System.Threading.Tasks;
using ILLVentApp.Domain.DTOs;
using ILLVentApp.Domain.Models;

namespace ILLVentApp.Domain.Interfaces
{
    public interface ICartService
    {
        Task<List<CartItemDto>> GetCartItemsAsync(string userId);
        Task<CartItemDto> AddToCartAsync(string userId, int productId, int quantity);
        Task<CartItemDto> UpdateCartItemAsync(int cartItemId, int quantity);
        Task<bool> RemoveFromCartAsync(int cartItemId);
        Task<bool> ClearCartAsync(string userId);
    }
} 