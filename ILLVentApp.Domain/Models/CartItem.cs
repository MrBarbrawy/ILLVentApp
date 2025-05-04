using System;

namespace ILLVentApp.Domain.Models
{
    public class CartItem
    {
        public int CartItemId { get; set; }
        public string UserId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Navigation properties
        public User User { get; set; }
        public Product Product { get; set; }
    }
} 