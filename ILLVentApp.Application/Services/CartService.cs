using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using ILLVentApp.Domain.DTOs;
using ILLVentApp.Domain.Interfaces;
using ILLVentApp.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ILLVentApp.Application.Services
{
    public class CartService : ICartService
    {
        private readonly IAppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<CartService> _logger;
        private const string AzureBaseUrl = "https://illventapp.azurewebsites.net";

        public CartService(
            IAppDbContext context, 
            IMapper mapper, 
            IHttpContextAccessor httpContextAccessor,
            ILogger<CartService> logger)
        {
            _context = context;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<List<CartItemDto>> GetCartItemsAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("GetCartItemsAsync called with null or empty userId");
                return new List<CartItemDto>();
            }

            var cartItems = await _context.CartItems
                .Include(ci => ci.Product)
                .Where(ci => ci.UserId == userId)
                .ToListAsync();

            var cartItemDtos = _mapper.Map<List<CartItemDto>>(cartItems);
            
            // Process URLs after mapping to DTOs
            foreach (var item in cartItemDtos)
            {
                ProcessImageUrl(item);
            }

            return cartItemDtos;
        }

        public async Task<CartItemDto> AddToCartAsync(string userId, int productId, int quantity)
        {
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("AddToCartAsync called with null or empty userId");
                return null;
            }

            if (quantity <= 0)
            {
                _logger.LogWarning("AddToCartAsync called with invalid quantity: {Quantity}", quantity);
                return null;
            }

            // Check if the product exists
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null)
            {
                _logger.LogWarning("Product with ID {ProductId} not found", productId);
                return null;
            }

            // Check if the product is already in the cart
            var existingCartItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.UserId == userId && ci.ProductId == productId);

            CartItem cartItem;

            if (existingCartItem != null)
            {
                // Update the quantity
                existingCartItem.Quantity += quantity;
                existingCartItem.UpdatedAt = DateTime.UtcNow;
                cartItem = existingCartItem;
            }
            else
            {
                // Add new cart item
                cartItem = new CartItem
                {
                    UserId = userId,
                    ProductId = productId,
                    Quantity = quantity,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();

            // Load the product for the cart item
            var loadedCartItem = await _context.CartItems
                .Include(ci => ci.Product)
                .FirstOrDefaultAsync(ci => ci.CartItemId == cartItem.CartItemId);

            var cartItemDto = _mapper.Map<CartItemDto>(loadedCartItem);
            ProcessImageUrl(cartItemDto);

            return cartItemDto;
        }

        public async Task<CartItemDto> UpdateCartItemAsync(int cartItemId, int quantity)
        {
            var cartItem = await _context.CartItems
                .Include(ci => ci.Product)
                .FirstOrDefaultAsync(ci => ci.CartItemId == cartItemId);

            if (cartItem == null)
            {
                _logger.LogWarning("CartItem with ID {CartItemId} not found", cartItemId);
                return null;
            }

            // Get the current user ID from HttpContext if available
            var currentUserId = _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;
            
            // Validate that the cart item belongs to the current user
            if (currentUserId != null && cartItem.UserId != currentUserId)
            {
                _logger.LogWarning("User {CurrentUserId} attempted to update CartItem {CartItemId} belonging to user {OwnerId}", 
                    currentUserId, cartItemId, cartItem.UserId);
                return null;
            }

            if (quantity <= 0)
            {
                // Remove the item if quantity is 0 or negative
                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();
                return null;
            }

            // Update the quantity
            cartItem.Quantity = quantity;
            cartItem.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Reload cart item with product
            var updatedCartItem = await _context.CartItems
                .Include(ci => ci.Product)
                .FirstOrDefaultAsync(ci => ci.CartItemId == cartItemId);

            var cartItemDto = _mapper.Map<CartItemDto>(updatedCartItem);
            ProcessImageUrl(cartItemDto);

            return cartItemDto;
        }

        public async Task<bool> RemoveFromCartAsync(int cartItemId)
        {
            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.CartItemId == cartItemId);

            if (cartItem == null)
            {
                _logger.LogWarning("CartItem with ID {CartItemId} not found", cartItemId);
                return false;
            }

            // Get the current user ID from HttpContext if available
            var currentUserId = _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;
            
            // Validate that the cart item belongs to the current user
            if (currentUserId != null && cartItem.UserId != currentUserId)
            {
                _logger.LogWarning("User {CurrentUserId} attempted to remove CartItem {CartItemId} belonging to user {OwnerId}", 
                    currentUserId, cartItemId, cartItem.UserId);
                return false;
            }

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ClearCartAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("ClearCartAsync called with null or empty userId");
                return false;
            }

            var cartItems = await _context.CartItems
                .Where(ci => ci.UserId == userId)
                .ToListAsync();

            if (cartItems.Count == 0)
            {
                return false;
            }

            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            return true;
        }

        private void ProcessImageUrl(CartItemDto itemDto)
        {
            if (itemDto == null)
            {
                return;
            }
            
            // Handle source directory paths in the DTO
            if (!string.IsNullOrEmpty(itemDto.ProductImage) && !itemDto.ProductImage.StartsWith("http"))
            {
                itemDto.ProductImage = $"{AzureBaseUrl}{itemDto.ProductImage}";
            }
        }
    }
} 