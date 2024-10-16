using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using e_commerce.Models;
using e_commerce.Data;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Security.Claims;


namespace e_commerce.Controllers
{
    
    public class ProductController : Controller
    {
        private readonly IMongoDBRepository<Product> _productRepository;
        private readonly IMongoDBRepository<ProductComment> _commentRepository;

        private readonly ILogger<ProductController> _logger;

        public ProductController(
            IMongoDBRepository<Product> productRepository,
            IMongoDBRepository<ProductComment> commentRepository,
            ILogger<ProductController> logger)
        {
            _productRepository = productRepository;
            _commentRepository = commentRepository;
            _logger = logger;
        }

        [Authorize(Roles = "ProductManager")]
        public async Task<IActionResult> Index()
        {
            var products = await _productRepository.GetAllAsync();
            return View(products);
        }

        [HttpGet]
        [Authorize(Roles = "ProductManager")]
        public IActionResult ManageProduct()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "ProductManager")]
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
        [Authorize(Roles = "ProductManager")]
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
        [Authorize(Roles = "ProductManager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            //if (ModelState.IsValid)
            //{
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
            //}
            
            

            return View(product);
        }
        [HttpGet]
        [Authorize(Roles = "ProductManager")]
        public async Task<IActionResult> Delete(string id)
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
        [ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var product = await _productRepository.FindByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            try
            {
                await _productRepository.DeleteOneAsync(id);
                _logger.LogInformation($"Product deleted successfully: {id}");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while deleting product: {id}");
                ModelState.AddModelError("", "An error occurred while deleting the product. Please try again.");
                return View(product);
            }
        }
       [AllowAnonymous]
        public async Task<IActionResult> ProductDetails(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            var comments = await _commentRepository.FilterByAsync(c => c.ProductId == id);
            ViewBag.Comments = comments;

            return View(product);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(string ProductId, string CommentText)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "User", new { returnUrl = Url.Action("ProductDetails", "Product", new { id = ProductId }) });
            }

            if (string.IsNullOrEmpty(ProductId) || string.IsNullOrEmpty(CommentText))
            {
                return BadRequest();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.Identity.Name;

            var comment = new ProductComment
            {
                ProductId = ProductId,
                UserId = userId,
                UserName = userName,
                CommentText = CommentText,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                await _commentRepository.InsertOneAsync(comment);
                _logger.LogInformation($"Comment added successfully for product {ProductId} by user {userId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding comment for product {ProductId} by user {userId}");
                TempData["ErrorMessage"] = "An error occurred while adding your comment. Please try again.";
            }

            return RedirectToAction(nameof(ProductDetails), new { id = ProductId });
        }
    }
    
}