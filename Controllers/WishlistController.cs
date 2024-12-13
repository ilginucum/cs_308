using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using e_commerce.Models;
using e_commerce.Data;
using System.Threading.Tasks;
using System.Security.Claims;
using static e_commerce.Models.WishlistItem;

namespace e_commerce.Controllers
{
    [Authorize]
    public class WishlistController : Controller
    {
        private readonly ILogger<SalesController> _salesRepository;
        private readonly IMongoDBRepository<WishlistItem> _wishlistRepository;
        private readonly IMongoDBRepository<Product> _productRepository;
        private readonly IMongoDBRepository<ShoppingCart> _shoppingCartRepository;

        public WishlistController(
            IMongoDBRepository<WishlistItem> wishlistRepository,
            IMongoDBRepository<Product> productRepository,
            IMongoDBRepository<ShoppingCart> shoppingCartRepository, ILogger<SalesController> salesRepository)
        {
            _wishlistRepository = wishlistRepository;
            _productRepository = productRepository;
            _shoppingCartRepository = shoppingCartRepository;
            _salesRepository = salesRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Fetch all wishlist items for the user
            var wishlistItems = await _wishlistRepository.FilterByAsync(w => w.UserId == userId);

            // Add QuantityInStock for each item
            var wishlistViewModel = new List<WishlistViewModel>();
            foreach (var item in wishlistItems)
            {
                var product = await _productRepository.FindByIdAsync(item.ProductId);
                var quantityInStock = product?.QuantityInStock ?? 0;

                wishlistViewModel.Add(new WishlistViewModel
                {
                    WishlistItem = item,
                    QuantityInStock = quantityInStock
                });
            }

            return View(wishlistViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> AddToWishlist(string productId, string productName, string author, decimal price, string imageUrl)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Check if the product is already in the wishlist
            var existingItem = await _wishlistRepository.FindOneAsync(w => w.ProductId == productId && w.UserId == userId);
            if (existingItem != null)
            {
                TempData["InfoMessage"] = "Product is already in your wishlist.";
                return RedirectToAction("Index", "Home");
            }

            // Add a new item to the wishlist
            var wishlistItem = new WishlistItem
            {
                ProductId = productId,
                ProductName = productName,
                Author = author,
                Price = price,
                ImageUrl = imageUrl,
                UserId = userId
            };

            await _wishlistRepository.InsertOneAsync(wishlistItem);

            TempData["SuccessMessage"] = $"{productName} has been added to your wishlist!";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromWishlist(string productId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Find and delete the wishlist item
            var item = await _wishlistRepository.FindOneAsync(w => w.ProductId == productId && w.UserId == userId);
            if (item != null)
            {
                await _wishlistRepository.DeleteOneAsync(item.Id);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> MoveToCart(string productId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Find the wishlist item
            var wishlistItem = await _wishlistRepository.FindOneAsync(w => w.ProductId == productId && w.UserId == userId);
            if (wishlistItem == null)
            {
                TempData["ErrorMessage"] = "Item not found in your wishlist.";
                return RedirectToAction(nameof(Index));
            }

            // Check product stock
            var product = await _productRepository.FindByIdAsync(productId);
            if (product == null || product.QuantityInStock <= 0)
            {
                TempData["ErrorMessage"] = "Item is out of stock and cannot be added to the cart.";
                return RedirectToAction(nameof(Index));
            }

            // Add the item to the shopping cart
            var cartItem = new CartItem
            {
                ProductId = wishlistItem.ProductId,
                ProductName = wishlistItem.ProductName,
                UnitPrice = wishlistItem.Price,
                QuantityInCart = 1 // Default quantity
            };

            var shoppingCart = await _shoppingCartRepository.FindOneAsync(s => s.UserId == userId);
            if (shoppingCart == null)
            {
                shoppingCart = new ShoppingCart
                {
                    UserId = userId,
                    Items = new List<CartItem> { cartItem }
                };

                await _shoppingCartRepository.InsertOneAsync(shoppingCart);
            }
            else
            {
                shoppingCart.Items.Add(cartItem);
                await _shoppingCartRepository.ReplaceOneAsync(shoppingCart);
            }

            // Remove the item from the wishlist
            await _wishlistRepository.DeleteOneAsync(wishlistItem.Id);

            TempData["SuccessMessage"] = $"{wishlistItem.ProductName} has been moved to your shopping cart!";
            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        public async Task<IActionResult> NotifyDiscountedProducts()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var wishlistItems = await _wishlistRepository.FilterByAsync(w => w.UserId == userId);

            var notifications = new List<string>();

            foreach (var item in wishlistItems)
            {
                if (item.DiscountedPrice < item.OriginalPrice)
                {
                    var discountPercentage = Math.Round((1 - (item.DiscountedPrice / item.OriginalPrice)) * 100);
                    notifications.Add($"{item.ProductName} is now discounted by {discountPercentage}%!");
                }
            }

            TempData["DiscountNotifications"] = notifications;

            return RedirectToAction("Index");
        }


    }
}

