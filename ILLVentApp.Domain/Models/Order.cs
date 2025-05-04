using System;
using System.Collections.Generic;

namespace ILLVentApp.Domain.Models
{
    public class Order
    {
        public int OrderId { get; set; }
        public string UserId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; } // "Cash" or "Card"
        public string PaymentStatus { get; set; } // "Pending", "Completed", "Failed"
        public string ShippingAddress { get; set; }
        public decimal ShippingCost { get; set; }
        public string OrderStatus { get; set; } // "Pending", "Processing", "Shipped", "Delivered", "Cancelled"
        public string StripeSessionId { get; set; } // For Stripe payment integration
        public string StripePaymentIntentId { get; set; }
        
        // Navigation properties
        public User User { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; }
    }
} 