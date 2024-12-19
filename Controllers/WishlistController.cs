using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using e_commerce.Models;
using e_commerce.Data;
using System.Threading.Tasks;
using System.Security.Claims;
using static e_commerce.Models.WishlistItem;
using MongoDB.Bson;


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

            var wishlistItems = await _wishlistRepository.FilterByAsync(w => w.UserId == userId);
            var wishlistViewModel = new List<WishlistViewModel>();
            var discountNotifications = new List<string>();

            foreach (var item in wishlistItems)
            {
                var product = await _productRepository.FindByIdAsync(item.ProductId);
                

                if (product != null)
                {
                    // Debugging statements
                    Console.WriteLine($"Product Name: {product.Name}");
                    Console.WriteLine($"Product Price: {product.Price}");
                    Console.WriteLine($"Product DiscountedPrice: {product.DiscountedPrice}");
                    Console.WriteLine($"WishlistItem Price: {item.Price}");

                    if (product.DiscountedPrice.HasValue && product.DiscountedPrice.Value < product.OriginalPrice)
                    {
                        var discountPercentage = Math.Round((1 - (product.DiscountedPrice.Value / product.OriginalPrice)) * 100);
                        discountNotifications.Add($"{product.Name} is now {discountPercentage}% off! Old Price: ${product.OriginalPrice:F2}, Discounted Price: ${product.DiscountedPrice.Value:F2}");
                    }

                    wishlistViewModel.Add(new WishlistViewModel
                    {
                        WishlistItem = new WishlistItem
                        {
                            Id = item.Id,
                            ProductId = item.ProductId,
                            ProductName = product.Name,
                            Author = product.Author,
                            Price = product.DiscountedPrice ?? product.Price,
                            ImageUrl = item.ImageUrl,
                            OriginalPrice = product.OriginalPrice,
                            DiscountedPrice = product.DiscountedPrice
                        },
                        QuantityInStock = product.QuantityInStock
                    });
                }
                else
                {
                    Console.WriteLine($"Product with ID {item.ProductId} not found.");
                }
            }

            ViewBag.DiscountNotifications = discountNotifications;

            return View(wishlistViewModel);
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

            CartItem cartItem; // Declare cartItem here

            if (shoppingCart == null)
            {
                shoppingCart = new ShoppingCart
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    UserId = userId,
                    Items = new List<CartItem>() // Initialize empty list first
                };

                cartItem = new CartItem
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    ProductId = wishlistItem.ProductId,
                    ProductName = wishlistItem.ProductName,
                    UnitPrice = wishlistItem.Price,
                    ShoppingCartId = shoppingCart.Id,
                    QuantityInCart = 1 // Default quantity
                };

                shoppingCart.Items.Add(cartItem);
                await _shoppingCartRepository.InsertOneAsync(shoppingCart);
            }
            else
            {
                cartItem = new CartItem
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    ProductId = wishlistItem.ProductId,
                    ProductName = wishlistItem.ProductName,
                    UnitPrice = wishlistItem.Price,
                    ShoppingCartId = shoppingCart.Id,
                    QuantityInCart = 1 // Default quantity
                };

                shoppingCart.Items.Add(cartItem);
                await _shoppingCartRepository.ReplaceOneAsync(shoppingCart);
            }

            // Delete the item from the wishlist after moving it to the cart
            await _wishlistRepository.DeleteOneAsync(wishlistItem.Id);

            // Return a redirect after the operation completes
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
                if (item.DiscountedPrice.HasValue && item.DiscountedPrice.Value < item.OriginalPrice)
                {
                    
                    var discountPercentage = Math.Round((1 - (item.DiscountedPrice.Value / item.OriginalPrice)) * 100);

                    
                    notifications.Add($"{item.ProductName} is now {discountPercentage}% off! Old Price: ${item.OriginalPrice:F2}, New Price: ${item.DiscountedPrice.Value:F2}");
                }
            }

            
            TempData["DiscountNotifications"] = notifications;

            return RedirectToAction("Index");
        }




    }
}

