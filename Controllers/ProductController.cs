using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using e_commerce.Models;
using e_commerce.Data;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace e_commerce.Controllers
{
    [Authorize(Roles = "ProductManager")]
    public class ProductController : Controller
    {
        private readonly IMongoDBRepository<Product> _productRepository;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IMongoDBRepository<Product> productRepository, ILogger<ProductController> logger)
        {
            _productRepository = productRepository;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _productRepository.GetAllAsync();
            return View(products);
        }

        [HttpGet]
        public IActionResult ManageProduct()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageProduct(Product product)
        {
            _logger.LogInformation("ManageProduct POST method called");
            //if (ModelState.IsValid)
            //{
                _logger.LogInformation("Model is valid. Attempting to insert product");
                try
                {
                    await _productRepository.InsertOneAsync(product);
                    _logger.LogInformation("Product inserted successfully");
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while inserting product");
                    ModelState.AddModelError("", "An error occurred while saving the product. Please try again.");
                }
            //}
            //else
            //{
            //    _logger.LogWarning("Model state is invalid");
            //    foreach (var modelState in ModelState.Values)
            //    {
            //        foreach (var error in modelState.Errors)
            //        {
            //            _logger.LogWarning($"Validation error: {error.ErrorMessage}");
            //        }
            //    }
            //}
            return View(product);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var product = await _productRepository.FindByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _productRepository.ReplaceOneAsync(product);
                    _logger.LogInformation($"Product updated successfully: {product.Id}");
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error occurred while updating product: {product.Id}");
                    ModelState.AddModelError("", "An error occurred while updating the product. Please try again.");
                }
            }
            else
            {
                _logger.LogWarning("Model state is invalid for product update");
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        _logger.LogWarning($"Validation error: {error.ErrorMessage}");
                    }
                }
            }

            return View(product);
        }

        public async Task<IActionResult> Delete(string id)
        {
            var product = await _productRepository.FindByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            await _productRepository.DeleteOneAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
    
}