using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using e_commerce.Models;
using e_commerce.Data;
using e_commerce.Controllers;
using System.Threading.Tasks;
using System.Security.Claims;

namespace e_commerce.Controllers
{
    [Authorize]
    public class WishlistController : Controller
    {
        private readonly IMongoDBRepository<WishlistItem> _wishlistRepository;
        private readonly IMongoDBRepository<ShoppingCart> _shoppingCartRepository;

        public WishlistController(IMongoDBRepository<WishlistItem> wishlistRepository, IMongoDBRepository<ShoppingCart> shoppingCartRepository)
        {
            _wishlistRepository = wishlistRepository;
            _shoppingCartRepository = shoppingCartRepository;

        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var wishlistItems = await _wishlistRepository.FilterByAsync(w => w.UserId == userId);
            return View(wishlistItems);
        }

        [HttpPost]
        public async Task<IActionResult> AddToWishlist(string productId, string productName, string author, decimal price, string imageUrl)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var existingItem = await _wishlistRepository.FindOneAsync(w => w.ProductId == productId && w.UserId == userId);
            if (existingItem != null)
            {
                TempData["InfoMessage"] = "Product is already in your wishlist.";
                return RedirectToAction("Index", "Home");
            }

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
            var item = await _wishlistRepository.FindOneAsync(w => w.ProductId == productId && w.UserId == userId);

            if (item != null)
            {
                await _wishlistRepository.DeleteOneAsync(item.Id);
            }

            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
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

            // Find the existing shopping cart or create a new one
            var shoppingCart = await _shoppingCartRepository.FindOneAsync(c => c.UserId == userId) ?? new ShoppingCart
            {
                UserId = userId,
                Items = new List<CartItem>()
            };

            // Check if the product is already in the cart
            var existingCartItem = shoppingCart.Items.FirstOrDefault(item => item.ProductId == productId);
            if (existingCartItem != null)
            {
                existingCartItem.QuantityInCart += 1; // Increment quantity if already in cart
            }
            else
            {
                // Create a new CartItem and add it to the shopping cart
                var cartItem = new CartItem
                {
                    ProductId = wishlistItem.ProductId,
                    ProductName = wishlistItem.ProductName,
                    UnitPrice = wishlistItem.Price,
                    QuantityInCart = 1 // Default quantity
                };
                shoppingCart.Items.Add(cartItem);
            }

            // Insert or update the shopping cart
            if (string.IsNullOrEmpty(shoppingCart.Id))
            {
                // Insert the new shopping cart if it doesn't exist
                await _shoppingCartRepository.InsertOneAsync(shoppingCart);
            }
            else
            {
                // Update the existing shopping cart
                await _shoppingCartRepository.ReplaceOneAsync(shoppingCart);
            }

            // Remove the item from the wishlist
            await _wishlistRepository.DeleteOneAsync(wishlistItem.Id);

            TempData["SuccessMessage"] = $"{wishlistItem.ProductName} has been moved to your shopping cart!";
            return RedirectToAction(nameof(Index));
        }


    }
}
