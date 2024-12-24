using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using e_commerce.Models;
using e_commerce.Data;
using e_commerce.Services;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;




namespace e_commerce.Controllers
{
    [Authorize(Roles = "SalesManager")]
    public class SalesController : Controller
    {
        private readonly IMongoDBRepository<Product> _productRepository;
        private readonly ILogger<SalesController> _logger;
        private readonly IMongoDBRepository<Order> _orderRepository;
        private readonly IMongoDBRepository<WishlistItem> _wishlistRepository;
        private readonly IPdfService _pdfService;
        private readonly IMongoDBRepository<Address> _addressRepository;


        public SalesController(
            IMongoDBRepository<Product> productRepository, 
            IMongoDBRepository<Order> orderRepository,
            ILogger<SalesController> logger,
            IMongoDBRepository<WishlistItem> wishlistRepository, IPdfService pdfService, IMongoDBRepository<Address> addressRepository)
        {
            _productRepository = productRepository;
            _orderRepository = orderRepository;
            _logger = logger;
            _wishlistRepository = wishlistRepository;
            _pdfService = pdfService;
            _addressRepository = addressRepository;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var products = await _productRepository.GetAllAsync();
                var orders = await _orderRepository.GetAllAsync();
                if (products == null)
                {
                    _logger.LogWarning("No products found");
                    products = new List<Product>();
                }
                var refundRequests = (await _orderRepository.FilterByAsync(o => o.RefundRequested == true)).ToList();
                var viewModel = new SalesViewModel
                {
                    Products = products,
                    FilteredOrders = new List<Order>(),
                    Orders = orders,
                    RefundRequests = refundRequests
                };

                // Check for success message in TempData
                if (TempData["SuccessMessage"] != null)
                {
                    ViewBag.SuccessMessage = TempData["SuccessMessage"].ToString();
                }

                _logger.LogInformation($"Retrieved {products.Count()} products and {orders.Count()} orders");
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching products and orders");
                return View(new SalesViewModel());
            }
        }

        [HttpGet]
        public async Task<IActionResult> SetPrice(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var product = await _productRepository.FindByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> SetPrice(string id, decimal price)
        {
            var product = await _productRepository.FindByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // Negatif fiyat kontrolü eklendi
            if (price < 0)
            {
                ModelState.AddModelError("Price", "Price cannot be negative."); // Hata mesajı
                return View(product); // Sayfayı geri döner ve hata mesajını gösterir
            }

            try
            {
                // Update product price and reset any discounts
                product.Price = price;
                product.OriginalPrice = price; // Sync original price
                product.DiscountedPrice = null; // Explicitly clear discounted price
                await _productRepository.ReplaceOneAsync(product);

                _logger.LogInformation($"Price updated successfully for product: {id}");

                // Synchronize all wishlist items that reference this product
                var wishlistItems = await _wishlistRepository.FilterByAsync(w => w.ProductId == id);
                foreach (var wishlistItem in wishlistItems)
                {
                    wishlistItem.Price = product.Price;
                    wishlistItem.OriginalPrice = product.OriginalPrice;
                    wishlistItem.DiscountedPrice = null; // Explicitly clear discounted price in wishlist items
                    await _wishlistRepository.ReplaceOneAsync(wishlistItem);
                }

                TempData["SuccessMessage"] = $"Price for '{product.Name}' has been updated successfully to {price:C}";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while updating price for product: {id}");
                ModelState.AddModelError("", "An error occurred while updating the price. Please try again.");
                return View(product);
            }
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyDiscount(string id, int discount)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var product = await _productRepository.FindByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            if (product.DiscountedPrice.HasValue && product.DiscountedPrice.Value < product.OriginalPrice)
            {
                TempData["ErrorMessage"] = $"A discount of {Math.Round((1 - (product.DiscountedPrice.Value / product.OriginalPrice)) * 100)}% is already applied.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
               
                if (discount < 0 || discount > 100)
                {
                    ModelState.AddModelError("", "Discount must be between 0 and 100.");
                    return RedirectToAction(nameof(Index));
                }

                
                if (product.OriginalPrice == 0)
                {
                    product.OriginalPrice = product.Price;
                }

                
                product.DiscountedPrice = Math.Round(product.OriginalPrice* (1 - (discount / 100.0M)), 2);

                
                product.Price = product.DiscountedPrice ?? product.Price;

                await _productRepository.ReplaceOneAsync(product);

                _logger.LogInformation($"Discount of {discount}% applied to product: {id}");
                TempData["SuccessMessage"] = $"Discount of {discount}% applied to '{product.Name}'. New price: {product.DiscountedPrice:C}";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error applying discount to product: {id}");
                ModelState.AddModelError("", "An error occurred while applying the discount. Please try again.");
                return RedirectToAction(nameof(Index));
            }
        }


        [HttpPost]
        [Route("Sales/CalculateProfit")]
        public async Task<IActionResult> CalculateProfit(DateTime startDate, DateTime endDate)
        {
            try
            {
                var allOrders = await _orderRepository.GetAllAsync();
                
                // Filter orders by OrderDate within the specified range
                var filteredOrders = allOrders
                                     .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                                     .ToList();
                // Calculate total revenue and profit/loss
                decimal totalRevenue = filteredOrders.Sum(o => o.TotalAmount);
                decimal totalCost = filteredOrders.Sum(o => o.Items.Sum(i => i.Quantity * i.UnitPrice * 0.7M)); // Assuming 30% profit margin
                decimal profitLoss = totalRevenue - totalCost;
                var products = await _productRepository.GetAllAsync();
                var viewModel = new SalesViewModel
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    Products = products,
                    Orders = allOrders,
                    FilteredOrders = filteredOrders,
                    TotalRevenue = totalRevenue,
                    ProfitLoss = profitLoss,
                    RefundRequests = (await _orderRepository.FilterByAsync(o => o.RefundRequested == true)).ToList()

                };
                return View("Index", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating profit");
                return View("Index", new SalesViewModel());
            }
        }
        public async Task<IActionResult> CreateOrder(Order order, Address newAddress)
        {
            try
            {
                // If new address is provided, save it to the Address collection
                if (!string.IsNullOrEmpty(newAddress.FullName))
                {
                    await _addressRepository.InsertOneAsync(newAddress);
                    order.AddressId = newAddress.Id; // Assign the new AddressId to the order
                }

                await _orderRepository.InsertOneAsync(order);

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                return StatusCode(500, "An error occurred while creating the order.");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GenerateInvoicePDF(string id, bool download = true)
        {
            var order = await _orderRepository.FindByIdAsync(id);

            if (order == null)
            {
                return NotFound("Order not found.");
            }

            // Fetch the address using AddressId
            var address = await _addressRepository.FindByIdAsync(order.AddressId);

            if (address == null)
            {
                _logger.LogWarning($"No address found with ID: {order.AddressId}");
                return NotFound("Shipping address not found.");
            }

            try
            {
                // Generate the PDF using the fetched address
                byte[] pdfBytes = await _pdfService.GenerateInvoicePdfAsync(order, address);
                if (download)
                {
                    // Download the PDF
                    Response.Headers["Content-Disposition"] = $"attachment; filename=Invoice_{order.Id}.pdf";
                }
                else
                {
                    // Display the PDF inline for printing
                    Response.Headers["Content-Disposition"] = "inline";
                }

                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating invoice PDF");
                return StatusCode(500, "An error occurred while generating the PDF.");
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetDiscount(string productId)
        {
            if (string.IsNullOrEmpty(productId))
            {
                return NotFound();
            }

            try
            {
                // Find the product by ID
                var product = await _productRepository.FindByIdAsync(productId);
                if (product == null)
                {
                    return NotFound("Product not found.");
                }

                // Reset the discount
                product.DiscountedPrice = null;
                product.Price = product.OriginalPrice;

                // Update the product in the database
                await _productRepository.ReplaceOneAsync(product);

                _logger.LogInformation($"Discount reset for product: {productId}");

                // Show a success message
                TempData["SuccessMessage"] = $"Discount for '{product.Name}' has been reset successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error resetting discount for product: {productId}");
                ModelState.AddModelError("", "An error occurred while resetting the discount. Please try again.");
                return RedirectToAction(nameof(Index));
            }
        }
        [HttpPost]
        public async Task<IActionResult> ApproveRefund(string orderId)
        {
            var order = await _orderRepository.FindByIdAsync(orderId);
            if (order != null)
            {
                order.RefundStatus = "Complete";
                await _orderRepository.UpdateOneAsync(orderId, order);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> RejectRefund(string orderId)
        {
            var order = await _orderRepository.FindByIdAsync(orderId);
            if (order != null)
            {
                order.RefundStatus = "Rejected";
                await _orderRepository.UpdateOneAsync(orderId, order);
            }
            return RedirectToAction("Index");
        }

    }
}