using System;

namespace ILLVentApp.Domain.DTOs
{
    public class CartItemDto
    {
        public int CartItemId { get; set; }
        public string UserId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductImage { get; set; }
        public decimal ProductPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
    }
} 