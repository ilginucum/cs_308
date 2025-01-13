using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using e_commerce.Models;
using e_commerce.Data;
using e_commerce.Controllers;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using e_commerce.Services;

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
        private readonly IMongoDBRepository<Category> _categoryRepository;
        private readonly IPdfService _pdfService;

        public ProductController(
            IMongoDBRepository<Product> productRepository,
            IMongoDBRepository<ProductComment> commentRepository,
            IMongoDBRepository<Order> orderRepository,
            IMongoDBRepository<Address> addressRepository,
            IMongoDBRepository<Rating> ratingRepository, // Rating repository ekleniyor
            IMongoDBRepository<WishlistItem> _wishlistRepository,
            IMongoDBRepository<Category> categoryRepository,
            IPdfService pdfService,
            ILogger<ProductController> logger)
        {
            _productRepository = productRepository;
            _commentRepository = commentRepository;
            _orderRepository = orderRepository;
            _addressRepository = addressRepository;
            _ratingRepository = ratingRepository;
            _categoryRepository = categoryRepository;
            _pdfService = pdfService;
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
            try
            {
                // Only get comments that are either:
                // 1. From users who have purchased the product and are pending
                // 2. Already approved comments
                var allComments = (await _commentRepository.GetAllAsync()).ToList();
                var viewModels = new List<CommentManagementViewModel>();
                
                foreach (var comment in allComments)
                {
                    var product = await _productRepository.FindByIdAsync(comment.ProductId);
                    
                    // Check if user has purchased the product
                    var orders = await _orderRepository.FilterByAsync(o => 
                        o.UserId == comment.UserId && 
                        o.OrderStatus == "Delivered" &&
                        o.Items.Any(item => item.ProductId == comment.ProductId));
                    
                    var hasPurchased = orders.Any();
                    
                    // Automatically reject comments from users who haven't purchased
                    if (!hasPurchased && comment.Status == "pending")
                    {
                        comment.Status = "rejected";
                        await _commentRepository.ReplaceOneAsync(comment);
                        _logger.LogInformation($"Comment {comment.Id} automatically rejected - no purchase verified");
                        continue; // Skip adding to viewModels
                    }
                    
                    // Only add to viewModel if:
                    // 1. Comment is pending AND user has purchased
                    // 2. Comment is already approved
                    if ((comment.Status == "pending" && hasPurchased) || comment.Status == "approved")
                    {
                        viewModels.Add(new CommentManagementViewModel
                        {
                            Id = comment.Id,
                            ProductId = comment.ProductId,
                            UserId = comment.UserId,
                            ProductName = product?.Name ?? "Unknown Product",
                            UserName = comment.UserName,
                            CommentText = comment.CommentText,
                            CreatedAt = comment.CreatedAt,
                            Status = comment.Status,
                            HasPurchased = hasPurchased
                        });
                    }
                }

                _logger.LogInformation($"Retrieved {viewModels.Count} comments for management");
                return View(viewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving comments for management");
                return View(new List<CommentManagementViewModel>());
            }
        }
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]


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
        public async Task<IActionResult> ManageProduct()
        {
            var categories = await _categoryRepository.GetAllAsync(); // MongoDB'den kategorileri al
            ViewBag.Categories = categories.Select(c => c.Name).ToList(); // ViewBag ile View'e gönder
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "ProductManager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageProduct(Product product)
        {
            _logger.LogInformation("ManageProduct POST method called");
            try
            {
                if (product.ImageFile != null && product.ImageFile.Length > 0)
                {
                    // Dosya adı olarak kitap adını kullanalım
                    var fileName = $"{product.Name.ToLower().Replace(" ", "-")}{Path.GetExtension(product.ImageFile.FileName)}";
                    
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                    var filePath = Path.Combine(uploadsFolder, fileName);
                    
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await product.ImageFile.CopyToAsync(fileStream);
                    }
                    
                    product.ImagePath = $"/images/{fileName}";
                }

                await _productRepository.InsertOneAsync(product);
                _logger.LogInformation("Product inserted successfully");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while inserting product");
                ModelState.AddModelError("", "An error occurred while saving the product. Please try again.");
            }

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

            try
            {
                // Yeni fotoğraf yüklendiyse
                if (product.ImageFile != null && product.ImageFile.Length > 0)
                {
                    // Eski fotoğrafı sil (eğer varsa)
                    var existingProduct = await _productRepository.FindByIdAsync(id);
                    if (!string.IsNullOrEmpty(existingProduct.ImagePath))
                    {
                        var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", 
                            existingProduct.ImagePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    // Yeni fotoğrafı yükle
                    var fileName = $"{product.Name.ToLower().Replace(" ", "-")}{Path.GetExtension(product.ImageFile.FileName)}";
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                    var filePath = Path.Combine(uploadsFolder, fileName);
                    
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await product.ImageFile.CopyToAsync(fileStream);
                    }
                    
                    product.ImagePath = $"/images/{fileName}";
                }
                else
                {
                    // Eğer yeni fotoğraf yüklenmediyse, mevcut fotoğraf yolunu koru
                    var existingProduct = await _productRepository.FindByIdAsync(id);
                    product.ImagePath = existingProduct.ImagePath;
                }

                await _productRepository.ReplaceOneAsync(product);
                _logger.LogInformation($"Product updated successfully: {product.Id}");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while updating product: {product.Id}");
                ModelState.AddModelError("", "An error occurred while updating the product. Please try again.");
            }
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

            // Get purchase status if user is logged in
            bool hasPurchased = false;
            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var orders = await _orderRepository.FilterByAsync(o => 
                    o.UserId == userId && 
                    o.OrderStatus == "Delivered" &&
                    o.Items.Any(item => item.ProductId == id));
                
                hasPurchased = orders.Any();
            }

            // Get comments and ratings
            var comments = await _commentRepository.FilterByAsync(c => c.ProductId == id && c.Status == "approved");
            var ratings = await _ratingRepository.FilterByAsync(r => r.ProductId == id);
            
            int totalRatings = ratings.Count();
            int totalComments = comments.Count();
            double averageScore = totalRatings > 0 ? ratings.Average(r => r.Score) : 0;

            var ratingDistribution = new Dictionary<int, int> { {1, 0}, {2, 0}, {3, 0}, {4, 0}, {5, 0} };

            foreach (var rating in ratings)
            {
                if (ratingDistribution.ContainsKey(rating.Score))
                {
                    ratingDistribution[rating.Score]++;
                }
            }

            ViewBag.HasPurchased = hasPurchased;
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
        
        // Check if user has purchased the product and if it's delivered
        var orders = await _orderRepository.FilterByAsync(o => 
            o.UserId == userId && 
            o.OrderStatus == "Delivered" &&
            o.Items.Any(item => item.ProductId == ProductId));
        
        if (!orders.Any())
        {
            TempData["ErrorMessage"] = "You can only comment on products you have purchased and received.";
            return RedirectToAction(nameof(ProductDetails), new { id = ProductId });
        }

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
            _logger.LogInformation($"Comment added and pending approval for product {ProductId} by user {userId}");
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
            
            // Check if user has purchased the product
            var orders = await _orderRepository.FilterByAsync(o => 
                o.UserId == userId && 
                o.OrderStatus == "Delivered" &&
                o.Items.Any(item => item.ProductId == ProductId));
            
            if (!orders.Any())
            {
                TempData["ErrorMessage"] = "You can only rate products you have purchased.";
                return RedirectToAction(nameof(ProductDetails), new { id = ProductId });
            }

            var userName = User.Identity.Name;

            // Check for existing rating
            var existingRating = await _ratingRepository.FindOneAsync(r => r.ProductId == ProductId && r.UserId == userId);

            if (existingRating != null)
            {
                existingRating.Score = selectedRating;
                existingRating.CreatedAt = DateTime.UtcNow;
                await _ratingRepository.ReplaceOneAsync(existingRating);
                _logger.LogInformation($"Rating updated for product {ProductId} by user {userId}");
            }
            else
            {
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
                    
                    ProcessingOrders = orders.Where(o => o.OrderStatus == "Pending").ToList(),
                    InTransitOrders = orders.Where(o => o.OrderStatus == "InTransit").ToList(),
                    DeliveredOrders = orders.Where(o => o.OrderStatus == "Delivered").ToList(),
                    Addresses = addresses.ToDictionary(a => a.Id, a => a)
                };

                
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

        [HttpGet]
        [Authorize(Roles = "ProductManager")]
        public async Task<IActionResult> CreateCategory()
        {
            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = categories.Select(c => c.Name).ToList();
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "ProductManager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(Category category)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Zaten var olan kategoriyi kontrol et
                    var existingCategory = await _categoryRepository.FindOneAsync(c => c.Name.ToLower() == category.Name.ToLower());
                    if (existingCategory != null)
                    {
                        // Eğer kategori zaten varsa hata mesajı ekle
                        ModelState.AddModelError("", "This category/genre already exists.");
                        return View(category);
                    }
                    // Category koleksiyonuna ekle
                    await _categoryRepository.InsertOneAsync(category);
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while adding new category.");
                    ModelState.AddModelError("", "Error occurred while adding category.");
                }
            }
            return View(category);
        }

        [HttpPost]
        [Authorize(Roles = "ProductManager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(string categoryName)
        {
            if (string.IsNullOrEmpty(categoryName))
            {
                ModelState.AddModelError("", "Please select a category to delete.");
                return RedirectToAction(nameof(CreateCategory));
            }

            try
            {
                // Kategoriyi MongoDB'den sil
                var category = await _categoryRepository.FindOneAsync(c => c.Name.ToLower() == categoryName.ToLower());
                if (category == null)
                {
                    ModelState.AddModelError("", "Category not found.");
                    return RedirectToAction(nameof(CreateCategory));
                }

                await _categoryRepository.DeleteOneAsync(category.Id);
                TempData["SuccessMessage"] = $"Category '{categoryName}' deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting category.");
                TempData["ErrorMessage"] = "An error occurred while deleting the category.";
            }

            return RedirectToAction(nameof(CreateCategory));
        }
        public void UpdateProductPrice(Product product, decimal newPrice, decimal discountPercentage = 0)
        {
            
            product.OriginalPrice = newPrice;

            
            if (discountPercentage > 0)
            {
                product.DiscountedPrice = product.OriginalPrice * (1 - discountPercentage / 100);
            }
            else
            {
                product.DiscountedPrice = null; 
            }

            
            _productRepository.ReplaceOneAsync(product);
        }

        [Authorize(Roles = "ProductManager")]
        public async Task<IActionResult> InvoiceManagement()
        {
            try
            {
                var orders = await _orderRepository.GetAllAsync();
                return View(orders.OrderByDescending(o => o.OrderDate));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders for invoice management");
                return View(Enumerable.Empty<Order>());
            }
        }

        [Authorize(Roles = "ProductManager")]
        public async Task<IActionResult> DownloadInvoice(string orderId)
        {
            try
            {
                var order = await _orderRepository.FindByIdAsync(orderId);
                if (order == null)
                {
                    return NotFound();
                }

                // Get shipping address
                var address = await _addressRepository.FindByIdAsync(order.AddressId);
                if (address == null)
                {
                    return NotFound("Shipping address not found");
                }

                var pdfBytes = await _pdfService.GenerateInvoicePdfAsync(order, address);
                return File(pdfBytes, "application/pdf", $"Invoice_{order.Id}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating invoice PDF for order {OrderId}", orderId);
                TempData["ErrorMessage"] = "Error generating invoice PDF.";
                return RedirectToAction(nameof(InvoiceManagement));
            }
        }
        [Authorize(Roles = "ProductManager")]
        public async Task<IActionResult> ViewInvoice(string orderId)
        {
            try
            {
                var order = await _orderRepository.FindByIdAsync(orderId);
                if (order == null)
                {
                    return NotFound();
                }

                var address = await _addressRepository.FindByIdAsync(order.AddressId);
                if (address == null)
                {
                    return NotFound("Shipping address not found");
                }

                var pdfBytes = await _pdfService.GenerateInvoicePdfAsync(order, address);
                return File(pdfBytes, "application/pdf", null); // null filename means display in browser
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating invoice PDF for order {OrderId}", orderId);
                TempData["ErrorMessage"] = "Error generating invoice PDF.";
                return RedirectToAction(nameof(InvoiceManagement));
            }
        }

        public async Task UpdateStockAsync(string productId, int quantity)
        {
            var product = await _productRepository.FindByIdAsync(productId);
            if (product != null)
            {
                product.QuantityInStock += quantity; // İptal edilen sipariş miktarını stoka geri ekle
                await _productRepository.ReplaceOneAsync(product);
            }
        }



    }


}
    
