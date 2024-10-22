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

        public ShoppingCartController(
            IMongoDBRepository<ShoppingCart> shoppingCartRepository,
            IMongoDBRepository<Product> productRepository,
            ILogger<ShoppingCartController> logger)
        {
            _shoppingCartRepository = shoppingCartRepository;
            _productRepository = productRepository;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = await GetOrCreateCartAsync(userId);

            foreach (var item in cart.Items)
            {
                if (item.ProductName == null)
                {
                    var product = await _productRepository.GetByIdAsync(item.ProductId);
                    item.ProductName = product?.Name ?? "Unknown Product";
                }
            }

            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(string productId, int quantity)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
            {
                return NotFound();
            }

            var cart = await GetOrCreateCartAsync(userId);
            var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);

            if (existingItem != null)
            {
                existingItem.QuantityInCart += quantity;
                existingItem.ProductName = product.Name; // Ürün adını güncelle
            }
            else
            {
                var newItem = new CartItem
                {
                    ProductId = productId,
                    QuantityInCart = quantity,
                    UnitPrice = product.Price,
                    ProductName = product.Name // Ürün adını ekle
                };
                cart.Items.Add(newItem);
            }

            cart.UpdatedAt = DateTime.UtcNow;
            await _shoppingCartRepository.ReplaceOneAsync(cart);

            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(string cartItemId, int change)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cart = await GetOrCreateCartAsync(userId);
            
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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = await GetOrCreateCartAsync(userId);
            var itemToRemove = cart.Items.FirstOrDefault(i => i.Id == cartItemId);
            if (itemToRemove != null)
            {
                cart.Items.Remove(itemToRemove);
                cart.UpdatedAt = DateTime.UtcNow;
                await _shoppingCartRepository.ReplaceOneAsync(cart);
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<ShoppingCart> GetOrCreateCartAsync(string userId)
        {
            var cart = await _shoppingCartRepository.FindOneAsync(c => c.UserId == userId);
            if (cart == null)
            {
                cart = new ShoppingCart { UserId = userId, Id = ObjectId.GenerateNewId().ToString() };
                await _shoppingCartRepository.InsertOneAsync(cart);
            }
            return cart;
        }

        private async Task AddToCartAsync(string userId, string name, string productId, decimal price, int quantity)
        {
            var cart = await GetOrCreateCartAsync(userId);
            var existingItem = cart.Items.Find(i => i.ProductId == productId);

            if (existingItem != null)
            {
                existingItem.QuantityInCart += quantity;
            }
            else
            {
                var newItem = new CartItem
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    ProductId = productId,
                    ShoppingCartId = cart.Id,
                    ProductName = name,
                    QuantityInCart = quantity,
                    UnitPrice = price
                };
                cart.Items.Add(newItem);
            }

            cart.UpdatedAt = DateTime.UtcNow;
            await _shoppingCartRepository.ReplaceOneAsync(cart);
        }

        private async Task RemoveFromCartAsync(string userId, string cartItemId)
        {
            var cart = await GetOrCreateCartAsync(userId);
            cart.Items.RemoveAll(i => i.Id == cartItemId);
            cart.UpdatedAt = DateTime.UtcNow;
            await _shoppingCartRepository.ReplaceOneAsync(cart);
        }
    }
}