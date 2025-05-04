using AutoMapper;
using ILLVentApp.Domain.DTOs;
using ILLVentApp.Domain.Interfaces;
using ILLVentApp.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;

namespace ILLVentApp.Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly IAppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICartService _cartService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OrderService> _logger;
        private const string AzureBaseUrl = "https://illventapp.azurewebsites.net";
        private const decimal DefaultShippingCost = 35m; // Default shipping cost in EGP

        public OrderService(
            IAppDbContext context, 
            IMapper mapper, 
            IHttpContextAccessor httpContextAccessor,
            ICartService cartService,
            IConfiguration configuration,
            ILogger<OrderService> logger)
        {
            _context = context;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _cartService = cartService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<List<OrderDto>> GetUserOrdersAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("GetUserOrdersAsync called with null or empty userId");
                return new List<OrderDto>();
            }

            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            // Verify payment status for any pending Card payments
            foreach (var order in orders)
            {
                if (order.PaymentMethod == "Card" && order.PaymentStatus == "Pending"
                    && !string.IsNullOrEmpty(order.StripeSessionId))
                {
                    await VerifyStripePaymentStatus(order);
                }
            }

            var orderDtos = _mapper.Map<List<OrderDto>>(orders);
            
            // Process URLs after mapping to DTOs
            foreach (var orderDto in orderDtos)
            {
                foreach (var item in orderDto.OrderItems)
                {
                    ProcessImageUrl(item);
                }
            }

            return orderDtos;
        }

        public async Task<OrderDto> GetOrderByIdAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                _logger.LogWarning("Order with ID {OrderId} not found", orderId);
                return null;
            }

            // Verify payment status for Card payments
            if (order.PaymentMethod == "Card" && order.PaymentStatus == "Pending" 
                && !string.IsNullOrEmpty(order.StripeSessionId))
            {
                await VerifyStripePaymentStatus(order);
            }

            var orderDto = _mapper.Map<OrderDto>(order);
            
            // Process URLs after mapping to DTOs
            foreach (var item in orderDto.OrderItems)
            {
                ProcessImageUrl(item);
            }

            return orderDto;
        }

        public async Task<OrderDto> CreateOrderFromCartAsync(string userId, CheckoutDto checkoutDto)
        {
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("CreateOrderFromCartAsync called with null or empty userId");
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
            }

            if (checkoutDto == null)
            {
                _logger.LogWarning("CreateOrderFromCartAsync called with null checkoutDto");
                throw new ArgumentNullException(nameof(checkoutDto));
            }

            if (checkoutDto.CartItemIds == null || checkoutDto.CartItemIds.Count == 0)
            {
                _logger.LogWarning("CreateOrderFromCartAsync called with empty cart items");
                throw new InvalidOperationException("No cart items provided for checkout");
            }

            // Begin transaction
            using var transaction = await _context.BeginTransactionAsync();

            try
            {
                // Get cart items
                var cartItems = await _context.CartItems
                    .Include(ci => ci.Product)
                    .Where(ci => ci.UserId == userId && checkoutDto.CartItemIds.Contains(ci.CartItemId))
                    .ToListAsync();

                if (cartItems.Count == 0)
                {
                    _logger.LogWarning("No cart items found for checkout. User: {UserId}, Items: {CartItemIds}", 
                        userId, string.Join(", ", checkoutDto.CartItemIds));
                    throw new InvalidOperationException("No cart items found for checkout");
                }

                // Calculate order totals
                decimal subtotal = cartItems.Sum(ci => ci.Product.Price * ci.Quantity);
                decimal shippingCost = DefaultShippingCost;
                decimal totalAmount = subtotal + shippingCost;

                // Create order
                var order = new Order
                {
                    UserId = userId,
                    OrderDate = DateTime.UtcNow,
                    TotalAmount = totalAmount,
                    PaymentMethod = checkoutDto.PaymentMethod,
                    PaymentStatus = "Pending",
                    ShippingAddress = checkoutDto.ShippingAddress,
                    ShippingCost = shippingCost,
                    OrderStatus = "Pending",
                    StripeSessionId = string.Empty,
                    StripePaymentIntentId = string.Empty,
                    OrderItems = new List<OrderItem>()
                };

                // Add order items
                foreach (var cartItem in cartItems)
                {
                    var orderItem = new OrderItem
                    {
                        ProductId = cartItem.ProductId,
                        Quantity = cartItem.Quantity,
                        UnitPrice = cartItem.Product.Price,
                        TotalPrice = cartItem.Product.Price * cartItem.Quantity
                    };

                    order.OrderItems.Add(orderItem);
                }

                // Save order to database
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Remove cart items
                _context.CartItems.RemoveRange(cartItems);
                await _context.SaveChangesAsync();

                // Commit transaction
                await transaction.CommitAsync();

                // Reload order with products for mapping
                order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.OrderId == order.OrderId);

                var orderDto = _mapper.Map<OrderDto>(order);

                // Process URLs after mapping to DTOs
                foreach (var item in orderDto.OrderItems)
                {
                    ProcessImageUrl(item);
                }

                return orderDto;
            }
            catch (Exception ex)
            {
                // Rollback transaction on error
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating order from cart for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, string status)
        {
            if (string.IsNullOrEmpty(status))
            {
                _logger.LogWarning("UpdateOrderStatusAsync called with null or empty status");
                return false;
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                _logger.LogWarning("Order with ID {OrderId} not found", orderId);
                return false;
            }

            order.OrderStatus = status;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Order {OrderId} status updated to {Status}", orderId, status);
            return true;
        }

        public async Task<bool> CancelOrderAsync(int orderId)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                _logger.LogWarning("Order with ID {OrderId} not found", orderId);
                return false;
            }

            // Only allow cancellation if order is pending
            if (order.OrderStatus != "Pending")
            {
                _logger.LogWarning("Cannot cancel order {OrderId} because it is in {Status} status", 
                    orderId, order.OrderStatus);
                return false;
            }

            order.OrderStatus = "Cancelled";
            
            // If order was paid with Stripe, process refund
            if (order.PaymentMethod == "Card" && !string.IsNullOrEmpty(order.StripePaymentIntentId))
            {
                try
                {
                    var refundService = new RefundService();
                    var refundOptions = new RefundCreateOptions
                    {
                        PaymentIntent = order.StripePaymentIntentId,
                        Reason = RefundReasons.RequestedByCustomer
                    };
                    
                    await refundService.CreateAsync(refundOptions);
                    order.PaymentStatus = "Refunded";
                    _logger.LogInformation("Refund processed for order {OrderId}", orderId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing refund for order {OrderId}", orderId);
                    // Continue with cancellation even if refund fails
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Order {OrderId} cancelled", orderId);
            return true;
        }

        public async Task<string> CreateStripeCheckoutSessionAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                _logger.LogWarning("Order with ID {OrderId} not found", orderId);
                throw new ArgumentException("Order not found");
            }

            // Create line items for Stripe checkout
            var lineItems = new List<SessionLineItemOptions>();
            
            foreach (var item in order.OrderItems)
            {
                if (item.Product == null)
                {
                    _logger.LogWarning("Product not found for order item {OrderItemId}", item.OrderItemId);
                    continue;
                }

                // Process image URL for Stripe
                var imageUrl = item.Product.ImageUrl;
                if (!string.IsNullOrEmpty(imageUrl) && !imageUrl.StartsWith("http"))
                {
                    imageUrl = $"{AzureBaseUrl}{imageUrl}";
                }

                lineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(item.UnitPrice * 100), // Stripe uses cents
                        Currency = "egp",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Product.Name,
                            Description = item.Product.Description,
                            Images = !string.IsNullOrEmpty(imageUrl) 
                                ? new List<string> { imageUrl } 
                                : null
                        }
                    },
                    Quantity = item.Quantity
                });
            }

            // Add shipping as a line item
            if (order.ShippingCost > 0)
            {
                lineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(order.ShippingCost * 100),
                        Currency = "egp",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "Shipping",
                            Description = "Shipping fee"
                        }
                    },
                    Quantity = 1
                });
            }

            // Create checkout session
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = lineItems,
                Mode = "payment",
                SuccessUrl = $"{AzureBaseUrl}/payment/success?session_id={{CHECKOUT_SESSION_ID}}",
                CancelUrl = $"{AzureBaseUrl}/payment/cancel?order_id={order.OrderId}",
                ClientReferenceId = order.OrderId.ToString(),
                CustomerEmail = order.User?.Email // Now this will be available because we included the User
            };

            try
            {
                var service = new SessionService();
                var session = await service.CreateAsync(options);

                // Update order with Stripe session ID
                order.StripeSessionId = session.Id;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Stripe checkout session created for order {OrderId}: {SessionId}", 
                    orderId, session.Id);
                return session.Url;
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error creating checkout session for order {OrderId}", orderId);
                throw new Exception("Error creating Stripe checkout session", ex);
            }
        }

        public async Task<bool> HandleStripeWebhookAsync(string payload, string signature)
        {
            if (string.IsNullOrEmpty(payload) || string.IsNullOrEmpty(signature))
            {
                _logger.LogWarning("Invalid webhook payload or signature");
                return false;
            }

            try
            {
                // Get the webhook secret from configuration
                string endpointSecret = _configuration["Stripe:WebhookSecret"];
                
                if (string.IsNullOrEmpty(endpointSecret))
                {
                    _logger.LogError("Stripe webhook secret not configured");
                    return false;
                }
                
                // Verify webhook signature
                var stripeEvent = EventUtility.ConstructEvent(
                    payload,
                    signature,
                    endpointSecret
                );

                _logger.LogInformation("Webhook received: {EventType} ({EventId})", 
                    stripeEvent.Type, stripeEvent.Id);

                // Handle the checkout.session.completed event
                if (stripeEvent.Type == "checkout.session.completed")
                {
                    var session = stripeEvent.Data.Object as Session;
                    
                    if (session == null)
                    {
                        _logger.LogWarning("Invalid session data in webhook");
                        return false;
                    }
                    
                    // Look up the order by Stripe session ID
                    var order = await _context.Orders
                        .FirstOrDefaultAsync(o => o.StripeSessionId == session.Id);

                    if (order != null)
                    {
                        // Update order status
                        order.PaymentStatus = "Completed";
                        order.OrderStatus = "Processing";
                        order.StripePaymentIntentId = session.PaymentIntentId;
                        
                        await _context.SaveChangesAsync();
                        
                        _logger.LogInformation("Order {OrderId} payment completed via Stripe", order.OrderId);
                    }
                    else
                    {
                        _logger.LogWarning("Order not found for Stripe session {SessionId}", session.Id);
                    }
                }
                
                return true;
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error processing webhook");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Stripe webhook");
                return false;
            }
        }

        private void ProcessImageUrl(OrderItemDto itemDto)
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

        private async Task VerifyStripePaymentStatus(Order order)
        {
            try
            {
                var sessionService = new SessionService();
                var session = await sessionService.GetAsync(order.StripeSessionId);
                
                if (session.PaymentStatus == "paid")
                {
                    // Update order if payment is completed
                    order.PaymentStatus = "Completed";
                    order.OrderStatus = "Processing";
                    
                    // Store the payment intent ID for potential refunds
                    if (!string.IsNullOrEmpty(session.PaymentIntentId) && 
                        string.IsNullOrEmpty(order.StripePaymentIntentId))
                    {
                        order.StripePaymentIntentId = session.PaymentIntentId;
                    }
                    
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Order {OrderId} payment status verified and updated via Stripe API", 
                        order.OrderId);
                }
            }
            catch (Exception ex)
            {
                // Log but don't throw - this is just a verification step
                _logger.LogWarning(ex, "Error verifying payment status for order {OrderId}", order.OrderId);
            }
        }
    }
} 