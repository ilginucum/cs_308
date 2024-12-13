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
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(
            IMongoDBRepository<Order> orderRepository,
            ILogger<OrdersController> logger)
        {
            _orderRepository = orderRepository;
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
    }
}