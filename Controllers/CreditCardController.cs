using Microsoft.AspNetCore.Mvc;
using e_commerce.Models;
using e_commerce.Data;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Linq;

namespace e_commerce.Controllers
{
    public class CreditCardController : Controller
{
    private readonly IMongoDBRepository<CreditCard> _creditCardRepository;
    private readonly IMongoDBRepository<Order> _orderRepository;
    private readonly IMongoDBRepository<ShoppingCart> _cartRepository;
    private readonly IMongoDBRepository<Product> _productRepository;
    private readonly ILogger<CreditCardController> _logger;

    public CreditCardController(
        IMongoDBRepository<CreditCard> creditCardRepository,
        IMongoDBRepository<Order> orderRepository,
        IMongoDBRepository<ShoppingCart> cartRepository,
        IMongoDBRepository<Product> productRepository,
        ILogger<CreditCardController> logger)
    {
        _creditCardRepository = creditCardRepository ?? throw new ArgumentNullException(nameof(creditCardRepository));
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _cartRepository = cartRepository ?? throw new ArgumentNullException(nameof(cartRepository));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet]
    public IActionResult EnterCreditCard()
    {
        var addressId = TempData["AddressId"]?.ToString();
        if (string.IsNullOrEmpty(addressId))
        {
            return RedirectToAction("Address", "Checkout");
        }

        TempData["AddressId"] = addressId;
        return View(new CreditCard());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCreditCard(CreditCard creditCard)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var addressId = TempData["AddressId"]?.ToString();
        if (string.IsNullOrEmpty(addressId))
        {
            return RedirectToAction("Address", "Checkout");
        }

        if (ModelState.IsValid)
        {
            try
            {
                // Get current cart
                var cart = await _cartRepository.FindOneAsync(c => c.UserId == userId);
                if (cart == null || !cart.Items.Any())
                {
                    return RedirectToAction("Index", "ShoppingCart");
                }

                // Check stock availability for all items
                foreach (var cartItem in cart.Items)
                {
                    var product = await _productRepository.GetByIdAsync(cartItem.ProductId);
                    if (product == null || product.QuantityInStock < cartItem.QuantityInCart)
                    {
                        ModelState.AddModelError("", $"Sorry, {product?.Name ?? "product"} is out of stock or has insufficient quantity.");
                        return View("EnterCreditCard", creditCard);
                    }
                }

                // Save credit card
                await _creditCardRepository.InsertOneAsync(creditCard);
                _logger.LogInformation("Credit card information saved successfully.");

                // Create order items and update stock
                var orderItems = new List<OrderItem>();
                foreach (var cartItem in cart.Items)
                {
                    var product = await _productRepository.GetByIdAsync(cartItem.ProductId);
                    
                    // Update product stock
                    product.QuantityInStock -= cartItem.QuantityInCart;
                    await _productRepository.ReplaceOneAsync(product);
                    _logger.LogInformation($"Updated stock for product {product.Id}. New stock: {product.QuantityInStock}");

                    orderItems.Add(new OrderItem
                    {
                        ProductId = cartItem.ProductId,
                        ProductName = cartItem.ProductName,
                        Quantity = cartItem.QuantityInCart,
                        UnitPrice = cartItem.UnitPrice,
                        TotalPrice = cartItem.UnitPrice * cartItem.QuantityInCart
                    });
                }

                // Create and save the order
                var order = new Order
                {
                    UserId = userId,
                    AddressId = addressId,
                    CreditCardId = creditCard.Id,
                    Items = orderItems,
                    TotalAmount = cart.TotalPrice,
                    OrderDate = DateTime.UtcNow,
                    OrderStatus = "Pending"
                };

                await _orderRepository.InsertOneAsync(order);
                _logger.LogInformation($"Order created successfully. Order ID: {order.Id}");

                // Clear the shopping cart
                await _cartRepository.DeleteOneAsync(cart.Id);
                _logger.LogInformation($"Shopping cart cleared for user {userId}");

                return RedirectToAction("Confirmation", new { orderId = order.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the order.");
                ModelState.AddModelError(string.Empty, "An error occurred while processing your order. Please try again.");
            }
        }

        return View("EnterCreditCard", creditCard);
    }

    public async Task<IActionResult> Confirmation(string orderId)
    {
        if (string.IsNullOrEmpty(orderId))
        {
            return RedirectToAction("Index", "Home");
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var order = await _orderRepository.GetByIdAsync(orderId);
        
        if (order == null || order.UserId != userId)
        {
            return NotFound();
        }

        return View(order);
    }
}
}