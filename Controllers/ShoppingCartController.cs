using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using e_commerce.Data;
using e_commerce.Models;
using System.Security.Claims;
using MongoDB.Bson;

namespace e_commerce.Controllers
{
    public class ShoppingCartController : Controller
    {
        
        private readonly IMongoDBRepository<ShoppingCart> _shoppingCartRepository;
        private readonly IMongoDBRepository<Product> _productRepository;
        private readonly ILogger<ShoppingCartController> _logger;
        private readonly IMongoDBRepository<WishlistItem> _wishlistRepository;


        public ShoppingCartController(
            IMongoDBRepository<ShoppingCart> shoppingCartRepository,
            IMongoDBRepository<Product> productRepository,
            ILogger<ShoppingCartController> logger, IMongoDBRepository<WishlistItem> wishlistRepository)
        {
            _shoppingCartRepository = shoppingCartRepository;
            _productRepository = productRepository;
            _logger = logger;
            
            _wishlistRepository = wishlistRepository;
        }

        public async Task<IActionResult> Index()
        {
            var cart = await GetOrCreateCartAsync();

            foreach (var item in cart.Items)
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId);
                if (product != null)
                {
                    item.ProductName = product.Name;
                    item.UnitPrice = decimal.Parse(product.Price.ToString("F2"));
                    item.ImagePath = product.ImagePath;
                }
                else
                {
                    item.ProductName = "Unknown Product";
                }
            }

            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(string productId, int quantity)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
            {
                return NotFound();
            }

            var cart = await GetOrCreateCartAsync();
            var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);

            if (existingItem != null)
            {
                existingItem.QuantityInCart += quantity;
                existingItem.ProductName = product.Name;
                existingItem.UnitPrice = product.Price;
            }
            else
            {
                var newItem = new CartItem
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    ProductId = productId,
                    QuantityInCart = quantity,
                    UnitPrice = decimal.Parse(product.Price.ToString("F2")),
                    ProductName = product.Name,
                    ShoppingCartId = cart.Id,
                    ImagePath = product.ImagePath
                };
                cart.Items.Add(newItem);
            }

            cart.UpdatedAt = DateTime.UtcNow;
            await _shoppingCartRepository.ReplaceOneAsync(cart);

            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        public async Task<IActionResult> MoveToCart(string productId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var wishlistItem = await _wishlistRepository.FindOneAsync(w => w.ProductId == productId && w.UserId == userId);
            if (wishlistItem == null)
            {
                TempData["ErrorMessage"] = "Item not found in your wishlist.";
                return RedirectToAction(nameof(Index));
            }

            var product = await _productRepository.FindByIdAsync(productId);
            if (product == null || product.QuantityInStock <= 0)
            {
                TempData["ErrorMessage"] = "Item is out of stock and cannot be added to the cart.";
                return RedirectToAction(nameof(Index));
            }

            var shoppingCart = await _shoppingCartRepository.FindOneAsync(s => s.UserId == userId);

            var cartItem = new CartItem
            {
                Id = ObjectId.GenerateNewId().ToString(), // Ensure a new unique ID
                ProductId = wishlistItem.ProductId,
                ProductName = wishlistItem.ProductName,
                UnitPrice = wishlistItem.Price,
                ShoppingCartId = shoppingCart?.Id ?? ObjectId.GenerateNewId().ToString(),
                QuantityInCart = 1 // Default quantity
            };

            if (shoppingCart == null)
            {
                shoppingCart = new ShoppingCart
                {
                    Id = ObjectId.GenerateNewId().ToString(),
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

            // Delete the item from the wishlist after moving it to the cart
            await _wishlistRepository.DeleteOneAsync(wishlistItem.Id);

            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(string cartItemId, int change)
        {
            var cart = await GetOrCreateCartAsync();

            var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId);
            if (item != null)
            {
                item.QuantityInCart += change;
                if (item.QuantityInCart <= 0)
                {
                    cart.Items.Remove(item);
                }

                cart.UpdatedAt = DateTime.UtcNow;
                await _shoppingCartRepository.ReplaceOneAsync(cart);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(string cartItemId)
        {
            var cart = await GetOrCreateCartAsync();
            var itemToRemove = cart.Items.FirstOrDefault(i => i.Id == cartItemId);

            if (itemToRemove != null)
            {
                cart.Items.Remove(itemToRemove);
                cart.UpdatedAt = DateTime.UtcNow;
                await _shoppingCartRepository.ReplaceOneAsync(cart);
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<ShoppingCart> GetOrCreateCartAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ShoppingCart cart;

            if (!string.IsNullOrEmpty(userId))
            {
                cart = await _shoppingCartRepository.FindOneAsync(c => c.UserId == userId);
                if (cart != null) return cart;
            }

            var sessionCartId = HttpContext.Session.GetString("CartSessionId");
            if (!string.IsNullOrEmpty(sessionCartId))
            {
                cart = await _shoppingCartRepository.FindOneAsync(c => c.SessionId == sessionCartId);
                if (cart != null) return cart;
            }

            sessionCartId = Guid.NewGuid().ToString();
            HttpContext.Session.SetString("CartSessionId", sessionCartId);

            cart = new ShoppingCart
            {
                Id = ObjectId.GenerateNewId().ToString(),
                UserId = userId,
                SessionId = userId == null ? sessionCartId : null,
                Items = new List<CartItem>()
            };

            await _shoppingCartRepository.InsertOneAsync(cart);
            return cart;
        }
        public async Task<bool> MergeGuestCart(string userId)
        {
            try
            {
                
                var sessionId = HttpContext.Session.GetString("CartSessionId");
                if (string.IsNullOrEmpty(sessionId))
                    return false;

                
                var guestCart = await _shoppingCartRepository.FindOneAsync(c => c.SessionId == sessionId);
                if (guestCart == null || !guestCart.Items.Any())
                    return false;

                
                var userCart = await _shoppingCartRepository.FindOneAsync(c => c.UserId == userId);
                if (userCart == null)
                {
                    
                    guestCart.UserId = userId;
                    guestCart.SessionId = null;
                    await _shoppingCartRepository.ReplaceOneAsync(guestCart);
                }
                else
                {
                    
                    foreach (var item in guestCart.Items)
                    {
                        var existingItem = userCart.Items.FirstOrDefault(i => i.ProductId == item.ProductId);
                        if (existingItem != null)
                        {
                            existingItem.QuantityInCart += item.QuantityInCart;
                        }
                        else
                        {
                            userCart.Items.Add(item);
                        }
                    }

                    userCart.UpdatedAt = DateTime.UtcNow;
                    await _shoppingCartRepository.ReplaceOneAsync(userCart);

                    
                    await _shoppingCartRepository.DeleteOneAsync(guestCart.Id);
                }

                
                HttpContext.Session.Remove("CartSessionId");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error merging guest cart");
                return false;
            }
        }

    };
};