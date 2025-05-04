using System;
using System.Threading.Tasks;
using ILLVentApp.Domain.DTOs;
using ILLVentApp.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace ILLVentApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUserOrders()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var orders = await _orderService.GetUserOrdersAsync(userId);
            return Ok(orders);
        }

        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetOrderById(int orderId)
        {
            var order = await _orderService.GetOrderByIdAsync(orderId);
            
            if (order == null)
                return NotFound();
                
            // Check if the order belongs to the current user or if user is admin
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            
            if (order.UserId != userId && !isAdmin)
                return Forbid();
                
            return Ok(order);
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout(CheckoutDto checkoutDto)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var order = await _orderService.CreateOrderFromCartAsync(userId, checkoutDto);
                
                // If payment method is card, create a Stripe checkout session
                if (checkoutDto.CreateOrder && order.PaymentMethod == "Card")
                {
                    var checkoutUrl = await _orderService.CreateStripeCheckoutSessionAsync(order.OrderId);
                    return Ok(new { OrderId = order.OrderId, CheckoutUrl = checkoutUrl });
                }
                
                return Ok(order);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPut("{orderId}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, [FromQuery] string status)
        {
            var result = await _orderService.UpdateOrderStatusAsync(orderId, status);
            
            if (!result)
                return NotFound();
                
            return NoContent();
        }

        [HttpPost("{orderId}/cancel")]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            // Verify order belongs to current user or user is admin
            var order = await _orderService.GetOrderByIdAsync(orderId);
            if (order == null)
                return NotFound();
                
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            
            if (order.UserId != userId && !isAdmin)
                return Forbid();
            
            var result = await _orderService.CancelOrderAsync(orderId);
            
            if (!result)
                return BadRequest("Order cannot be cancelled");
                
            return NoContent();
        }

        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> StripeWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var signature = Request.Headers["Stripe-Signature"];
            
            var success = await _orderService.HandleStripeWebhookAsync(json, signature);
            
            if (!success)
                return BadRequest();
                
            return Ok();
        }

        [HttpPost("{orderId}/verify-payment")]
        public async Task<IActionResult> VerifyPayment(int orderId)
        {
            // Get the order
            var order = await _orderService.GetOrderByIdAsync(orderId);
            
            if (order == null)
                return NotFound();
                
            // Check if the order belongs to the current user or if user is admin
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            
            if (order.UserId != userId && !isAdmin)
                return Forbid();
            
            // The GetOrderByIdAsync method now automatically verifies payment status
            // Just check if it's still pending
            if (order.PaymentStatus == "Pending" && order.PaymentMethod == "Card")
            {
                return Ok(new { 
                    message = "Payment verification initiated. Please check the order status again.",
                    order 
                });
            }
            
            return Ok(order);
        }
    }
} 