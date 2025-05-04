using System;
using System.Collections.Generic;

namespace ILLVentApp.Domain.DTOs
{
    public class CheckoutDto
    {
        public string PaymentMethod { get; set; }
        public string ShippingAddress { get; set; }
        public List<int> CartItemIds { get; set; }
        public bool CreateOrder { get; set; }
    }
} 