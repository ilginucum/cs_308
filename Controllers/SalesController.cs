using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using e_commerce.Models;
using e_commerce.Data;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace e_commerce.Controllers
{
    [Authorize(Roles = "SalesManager")]
    public class SalesController : Controller
    {
        private readonly IMongoDBRepository<Product> _productRepository;
        private readonly ILogger<SalesController> _logger;

        public SalesController(
            IMongoDBRepository<Product> productRepository,
            ILogger<SalesController> logger)
        {
            _productRepository = productRepository;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var products = await _productRepository.GetAllAsync();
                if (products == null)
                {
                    _logger.LogWarning("No products found");
                    products = new List<Product>();
                }
                
                // Check for success message in TempData
                if (TempData["SuccessMessage"] != null)
                {
                    ViewBag.SuccessMessage = TempData["SuccessMessage"].ToString();
                }
                
                _logger.LogInformation($"Retrieved {products.Count()} products");
                return View(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching products");
                return View(new List<Product>());
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
                
                // Add success message
                TempData["SuccessMessage"] = $"Price for '{product.Name}' has been updated successfully to {price:C}";
                
                // Redirect with query parameter for animation
                return RedirectToAction(nameof(Index), new { updated = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while updating price for product: {id}");
                ModelState.AddModelError("", "An error occurred while updating the price. Please try again.");
                return View(product);
            }
        }
    }
}