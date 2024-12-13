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
        private readonly ILogger<SalesController> _salesRepository;
        private readonly IMongoDBRepository<ShoppingCart> _shoppingCartRepository;
       private readonly IMongoDBRepository<Product> _productRepository;
        private readonly IMongoDBRepository<ShoppingCart> _wishlistRepository;
        private readonly ILogger<ShoppingCartController> _logger;

       public ShoppingCartController(
           IMongoDBRepository<ShoppingCart> shoppingCartRepository,
           IMongoDBRepository<Product> productRepository,
           ILogger<ShoppingCartController> logger, ILogger<SalesController> salesRepository)
       {
           _shoppingCartRepository = shoppingCartRepository;
           _productRepository = productRepository;
           _logger = logger;
           _salesRepository = salesRepository;
       }

       public async Task<IActionResult> Index()
       {
           var cart = await GetOrCreateCartAsync();

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
           }
           else
           {
               var newItem = new CartItem
               {
                   Id = ObjectId.GenerateNewId().ToString(),
                   ProductId = productId,
                   QuantityInCart = quantity,
                   UnitPrice = product.Price,
                   ProductName = product.Name,
                   ShoppingCartId = cart.Id
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


            public async Task<bool> MergeGuestCart(string userId)
        {
            try
            {
                // Session'dan guest cart'ı bul
                var sessionId = HttpContext.Session.GetString("CartSessionId");
                if (string.IsNullOrEmpty(sessionId))
                    return false;

                // Guest cart'ı bul
                var guestCart = await _shoppingCartRepository.FindOneAsync(c => c.SessionId == sessionId);
                if (guestCart == null || !guestCart.Items.Any())
                    return false;

                // User'ın cart'ını bul veya oluştur
                var userCart = await _shoppingCartRepository.FindOneAsync(c => c.UserId == userId);
                if (userCart == null)
                {
                    // Guest cart'ı user cart'ına çevir
                    guestCart.UserId = userId;
                    guestCart.SessionId = null;
                    await _shoppingCartRepository.ReplaceOneAsync(guestCart);
                }
                else
                {
                    // İki sepeti birleştir
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
                    
                    // Guest cart'ı sil
                    await _shoppingCartRepository.DeleteOneAsync(guestCart.Id);
                }

                // Session'ı temizle
                HttpContext.Session.Remove("CartSessionId");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error merging guest cart");
                return false;
            }
        }

       private async Task<ShoppingCart> GetOrCreateCartAsync()
       {
           var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
           ShoppingCart cart;

           // Önce eski mantıkla deneyelim (geriye uyumluluk için)
           if (!string.IsNullOrEmpty(userId))
           {
               cart = await _shoppingCartRepository.FindOneAsync(c => c.UserId == userId);
               if (cart != null) return cart;
           }

           // Eğer kullanıcı giriş yapmamışsa veya sepeti yoksa
           var sessionCartId = HttpContext.Session.GetString("CartSessionId");
           if (!string.IsNullOrEmpty(sessionCartId))
           {
               cart = await _shoppingCartRepository.FindOneAsync(c => c.SessionId == sessionCartId);
               if (cart != null) return cart;
           }

           // Hiçbir sepet bulunamadıysa yeni oluştur
           sessionCartId = Guid.NewGuid().ToString();
           HttpContext.Session.SetString("CartSessionId", sessionCartId);

           cart = new ShoppingCart 
           { 
               Id = ObjectId.GenerateNewId().ToString(),
               UserId = userId,  // Eğer login ise UserId set edilir, değilse null kalır
               SessionId = userId == null ? sessionCartId : null, // Login değilse SessionId set edilir
               Items = new List<CartItem>()
           };

           await _shoppingCartRepository.InsertOneAsync(cart);
           return cart;
       }
  

    }




}