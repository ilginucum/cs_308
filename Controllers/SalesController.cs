using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using e_commerce.Models;
using e_commerce.Data;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace e_commerce.Controllers
{
    [Authorize(Roles = "SalesManager")]
    public class SalesController : Controller
    {
        private readonly IMongoDBRepository<Order> _orderRepository;
        private readonly IMongoDBRepository<Product> _productRepository;
        private readonly ILogger<SalesController> _logger;

        public SalesController(
            IMongoDBRepository<Product> productRepository,
            IMongoDBRepository<Order> orderRepository,
            ILogger<SalesController> logger)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var products = await _productRepository.GetAllAsync();
                var orders = await _orderRepository.GetAllAsync();

                if (products == null)
                {
                    _logger.LogWarning("No products found");
                    products = new List<Product>();
                }

                var viewModel = new SalesViewModel
                {
                    Products = products,
                    Orders = orders
                };

                // Check for success message in TempData
                if (TempData["SuccessMessage"] != null)
                {
                    ViewBag.SuccessMessage = TempData["SuccessMessage"].ToString();
                }

                _logger.LogInformation($"Retrieved {products.Count()} products and {orders.Count()} orders");
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching products and orders");
                return View(new SalesViewModel());
            }
        }
        [HttpGet]
        public async Task<IActionResult> SetPrice(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var product = await _productRepository.FindByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPrice(string id, decimal price)
        {
            var product = await _productRepository.FindByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            try
            {
                product.Price = price;
                await _productRepository.ReplaceOneAsync(product);

                _logger.LogInformation($"Price updated successfully for product: {id}");
                TempData["SuccessMessage"] = $"Price for '{product.Name}' has been updated successfully to {price:C}";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while updating price for product: {id}");
                ModelState.AddModelError("", "An error occurred while updating the price. Please try again.");
                return View(product);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyDiscount(string id, int discount)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var product = await _productRepository.FindByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            try
            {
                if (discount < 0 || discount > 100)
                {
                    ModelState.AddModelError("", "Discount must be between 0 and 100.");
                    return RedirectToAction(nameof(Index));
                }

                // Calculate new discounted price
                product.Price = product.Price * (1 - (discount / 100.0M));
                await _productRepository.ReplaceOneAsync(product);

                _logger.LogInformation($"Discount of {discount}% applied to product: {id}");
                TempData["SuccessMessage"] = $"Discount of {discount}% applied to '{product.Name}'. New price: {product.Price:C}";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error applying discount to product: {id}");
                ModelState.AddModelError("", "An error occurred while applying the discount. Please try again.");
                return RedirectToAction(nameof(Index));
            }
        }


        [HttpPost]
        [Route("Sales/CalculateProfit")]
        public async Task<IActionResult> CalculateProfit(DateTime startDate, DateTime endDate)
        {
            try
            {
                var allOrders = await _orderRepository.GetAllAsync();

                // Filter orders by OrderDate within the specified range
                var filteredOrders = allOrders
                                     .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                                     .ToList();

                // Calculate total revenue and profit/loss
                decimal totalRevenue = filteredOrders.Sum(o => o.TotalAmount);
                decimal totalCost = filteredOrders.Sum(o => o.Items.Sum(i => i.Quantity * i.UnitPrice * 0.7M)); // Assuming 30% profit margin
                decimal profitLoss = totalRevenue - totalCost;

                var products = await _productRepository.GetAllAsync();

                var viewModel = new SalesViewModel
                {
                    Products = products,
                    Orders = allOrders,
                    FilteredOrders = filteredOrders,
                    TotalRevenue = totalRevenue,
                    ProfitLoss = profitLoss
                };

                return View("Index", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating profit");
                return View("Index", new SalesViewModel());
            }
        }
    }
}
