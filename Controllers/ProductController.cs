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
        private readonly IMongoDBRepository<Rating> _ratingRepository;
        private readonly IMongoDBRepository<Order> _orderRepository;
        private readonly IMongoDBRepository<Address> _addressRepository;

        private readonly ILogger<ProductController> _logger;

        public ProductController(
            IMongoDBRepository<Product> productRepository,
            IMongoDBRepository<ProductComment> commentRepository,
            IMongoDBRepository<Order> orderRepository,
        IMongoDBRepository<Address> addressRepository,
            IMongoDBRepository<Rating> ratingRepository, // Rating repository ekleniyor
            ILogger<ProductController> logger)
        {
            _productRepository = productRepository;
            _commentRepository = commentRepository;
            _orderRepository = orderRepository;
            _addressRepository = addressRepository;
            _ratingRepository = ratingRepository;
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
        public async Task<IActionResult> ManageComments()
        {
        var allComments = (await _commentRepository.GetAllAsync()).ToList();
        
        foreach (var comment in allComments)
        {
            var product = await _productRepository.FindByIdAsync(comment.ProductId);
            comment.ProductName = product?.Name ?? "Unknown Product";
        }

        return View(allComments);
        }

        [HttpPost]
        [Authorize(Roles = "ProductManager")]
        public async Task<IActionResult> ApproveComment(string commentId)
        {
            var comment = await _commentRepository.FindByIdAsync(commentId);
            if (comment != null)
            {
                comment.Status = "approved";
                await _commentRepository.ReplaceOneAsync(comment);
                _logger.LogInformation($"Comment approved: {commentId}");
            }
            return RedirectToAction("ManageComments");
        }

        [HttpPost]
        [Authorize(Roles = "ProductManager")]
        public async Task<IActionResult> RequestChange(string commentId)
        {
            var comment = await _commentRepository.FindByIdAsync(commentId);
            if (comment != null)
            {
                comment.Status = "needs_change";
                await _commentRepository.ReplaceOneAsync(comment);
                _logger.LogInformation($"Change requested for comment: {commentId}");
            }
            return RedirectToAction("ManageComments");
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

            // Yalnızca onaylanmış yorumları filtrele
            var comments = await _commentRepository.FilterByAsync(c => c.ProductId == id && c.Status == "approved");
            var ratings = await _ratingRepository.FilterByAsync(r => r.ProductId == id);
            
            int totalRatings = ratings.Count();
            int totalComments = comments.Count();
            double averageScore = totalRatings > 0 ? ratings.Average(r => r.Score) : 0;

            // Varsayılan rating dağılımını oluşturun
            var ratingDistribution = new Dictionary<int, int> { {1, 0}, {2, 0}, {3, 0}, {4, 0}, {5, 0} };

            // Mevcut rating'leri dağılıma ekleyin
            foreach (var rating in ratings)
            {
                if (ratingDistribution.ContainsKey(rating.Score))
                {
                    ratingDistribution[rating.Score]++;
                }
            }

            // ViewBag ile varsayılan değerleri ayarlayın
            ViewBag.AverageScore = averageScore;
            ViewBag.TotalRatings = totalRatings;
            ViewBag.TotalComments = totalComments;
            ViewBag.RatingDistribution = ratingDistribution;
            ViewBag.Comments = comments;

            return View(product);
        }


        [HttpPost]
        [Authorize(Roles = "Customer")]
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
            var product = await _productRepository.FindByIdAsync(ProductId);

            var comment = new ProductComment
            {
                ProductId = ProductId,
                ProductName = product?.Name,
                UserId = userId,
                UserName = userName,
                CommentText = CommentText,
                CreatedAt = DateTime.UtcNow,
                Status = "pending"
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
        
        [HttpPost]
        [Authorize(Roles = "Customer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddRating(string ProductId, int selectedRating)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "User", new { returnUrl = Url.Action("ProductDetails", "Product", new { id = ProductId }) });
            }

            if (string.IsNullOrEmpty(ProductId) || selectedRating < 1 || selectedRating > 5)
            {
                return BadRequest();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.Identity.Name;

            // Mevcut rating kontrolü
            var existingRating = await _ratingRepository.FindOneAsync(r => r.ProductId == ProductId && r.UserId == userId);

            if (existingRating != null)
            {
                // Eğer mevcut bir rating varsa, mevcut kaydı güncelleyin
                existingRating.Score = selectedRating;
                existingRating.CreatedAt = DateTime.UtcNow;
                await _ratingRepository.ReplaceOneAsync(existingRating);
                _logger.LogInformation($"Rating updated for product {ProductId} by user {userId}");
            }
            else
            {
                // Eğer mevcut bir rating yoksa, yeni bir kayıt oluşturun
                var rating = new Rating
                {
                    ProductId = ProductId,
                    UserId = userId,
                    UserName = userName,
                    Score = selectedRating,
                    CreatedAt = DateTime.UtcNow
                };

                await _ratingRepository.InsertOneAsync(rating);
                _logger.LogInformation($"Rating added successfully for product {ProductId} by user {userId}");
            }

            return RedirectToAction(nameof(ProductDetails), new { id = ProductId });
        }

            [Authorize(Roles = "ProductManager")]
        public async Task<IActionResult> OrderManagement()
        {
            try
            {
                var orders = (await _orderRepository.GetAllAsync()).ToList();
                var addresses = (await _addressRepository.GetAllAsync()).ToList();

                _logger.LogInformation($"Retrieved {orders.Count} orders and {addresses.Count} addresses");

                var orderViewModel = new OrderManagementViewModel
                {
                    // Use "Pending" instead of "Processing"
                    ProcessingOrders = orders.Where(o => o.OrderStatus == "Pending").ToList(),
                    InTransitOrders = orders.Where(o => o.OrderStatus == "InTransit").ToList(),
                    DeliveredOrders = orders.Where(o => o.OrderStatus == "Delivered").ToList(),
                    Addresses = addresses.ToDictionary(a => a.Id, a => a)
                };

                // Debug logging
                _logger.LogInformation($"Pending Orders: {orderViewModel.ProcessingOrders.Count}");
                _logger.LogInformation($"In Transit Orders: {orderViewModel.InTransitOrders.Count}");
                _logger.LogInformation($"Delivered Orders: {orderViewModel.DeliveredOrders.Count}");

                return View(orderViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders and addresses");
                throw;
            }
        }

        [HttpPost]
        [Authorize(Roles = "ProductManager")]
        public async Task<IActionResult> UpdateOrderStatus(string orderId, string newStatus)
        {
            try
            {
                var order = await _orderRepository.FindByIdAsync(orderId);
                if (order != null)
                {
                    _logger.LogInformation($"Updating order {orderId} status from {order.OrderStatus} to {newStatus}");
                    
                    // Convert "Processing" to "Pending" if that's the new status
                    order.OrderStatus = newStatus == "Processing" ? "Pending" : newStatus;
                    
                    await _orderRepository.ReplaceOneAsync(order);
                    _logger.LogInformation($"Successfully updated order {orderId} status to {order.OrderStatus}");
                }
                else
                {
                    _logger.LogWarning($"Order {orderId} not found");
                }
                return RedirectToAction(nameof(OrderManagement));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating order {orderId} status");
                throw;
            }
        }
    
        }


    }
    
