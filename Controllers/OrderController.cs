using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using e_commerce.Models;
using e_commerce.Data;
using System.Security.Claims;

namespace e_commerce.Controllers
{
    [Authorize(Roles = "Customer")]
    public class OrdersController : Controller
    {
        private readonly IMongoDBRepository<Order> _orderRepository;
        private readonly IMongoDBRepository<Product> _productRepository; // Ekle
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(
            IMongoDBRepository<Order> orderRepository,
            IMongoDBRepository<Product> productRepository, // Ekle
            ILogger<OrdersController> logger)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository; // Ekle
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Add debug logging
                _logger.LogInformation("Accessing Orders Index");
                
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                _logger.LogInformation($"Current UserId: {userId}");

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("UserId is null or empty");
                    return View(Enumerable.Empty<Order>());
                }

                var orders = await _orderRepository.FilterByAsync(o => o.UserId == userId);
                var ordersList = orders.ToList();


                _logger.LogInformation($"Found {ordersList.Count} orders for user");

                return View(ordersList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders");
                return View(Enumerable.Empty<Order>());
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestRefund(string orderId, string selectedProducts)
        {
            if (string.IsNullOrEmpty(orderId) || string.IsNullOrEmpty(selectedProducts))
            {
                return NotFound();
            }

            var order = await _orderRepository.FindByIdAsync(orderId);
            if (order == null)
            {
                return NotFound("Order not found.");
            }

            if (order.OrderStatus != "Delivered")
            {
                TempData["ErrorMessage"] = "You can only request refund for delivered orders.";
                return RedirectToAction(nameof(Index));
            }

            if ((DateTime.Now - order.OrderDate).TotalDays > 30)
            {
                TempData["ErrorMessage"] = "Refund can only be requested within 30 days of purchase.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var selectedProductIds = selectedProducts.Split(',');
                var successCount = 0;
                var failCount = 0;

                foreach (var productId in selectedProductIds)
                {
                    var item = order.Items.FirstOrDefault(i => i.ProductId == productId);
                    if (item != null && !item.RefundRequested)
                    {
                        item.RefundRequested = true;
                        successCount++;
                    }
                    else
                    {
                        failCount++;
                    }
                }

                if (successCount > 0)
                {
                    await _orderRepository.ReplaceOneAsync(order);
                    
                    var message = successCount == 1 
                        ? "Refund request has been submitted successfully." 
                        : $"Refund requests for {successCount} items have been submitted successfully.";
                    
                    if (failCount > 0)
                    {
                        message += $" ({failCount} items could not be processed)";
                    }
                    
                    TempData["SuccessMessage"] = message;
                }
                else
                {
                    TempData["ErrorMessage"] = "No valid items were found for refund request.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting refund for order: {orderId}, products: {selectedProducts}", 
                    orderId, selectedProducts);
                TempData["ErrorMessage"] = "An error occurred while requesting the refund.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(string orderId)
        {
            if (string.IsNullOrEmpty(orderId))
            {
                return NotFound();
            }

            var order = await _orderRepository.FindByIdAsync(orderId);
            if (order == null)
            {
                return NotFound("Order not found.");
            }

            // Sadece Pending durumundaki siparişler iptal edilebilir
            if (order.OrderStatus != "Pending")
            {
                TempData["ErrorMessage"] = "Only pending orders can be cancelled.";
                return RedirectToAction(nameof(Index));
            }

            try 
            {
                // Her bir ürün için stok güncellenmeli
                foreach (var item in order.Items)
                {
                    var product = await _productRepository.FindByIdAsync(item.ProductId);
                    if (product != null)
                    {
                        product.QuantityInStock += item.Quantity;
                        await _productRepository.ReplaceOneAsync(product);
                    }
                }

                // Siparişi sil
                await _orderRepository.DeleteOneAsync(orderId);
                
                TempData["SuccessMessage"] = "Order has been successfully cancelled.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order: {orderId}", orderId);
                TempData["ErrorMessage"] = "An error occurred while cancelling the order.";
                return RedirectToAction(nameof(Index));
            }
        }

    }
}