using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using e_commerce.Models;
using e_commerce.Data;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace e_commerce.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly IMongoDBRepository<Address> _addressRepository;
        private readonly IMongoDBRepository<ShoppingCart> _cartRepository;
        private readonly ILogger<CheckoutController> _logger;

        // Single constructor with all required dependencies
        public CheckoutController(
            IMongoDBRepository<Address> addressRepository,
            IMongoDBRepository<ShoppingCart> cartRepository,
            ILogger<CheckoutController> logger)
        {
            _addressRepository = addressRepository ?? throw new ArgumentNullException(nameof(addressRepository));
            _cartRepository = cartRepository ?? throw new ArgumentNullException(nameof(cartRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public IActionResult Address()
        {
            if (!User.Identity.IsAuthenticated)
            {
                // Login'e yönlendir ve dönüş URL'ini kaydet
                return RedirectToAction("Login", "User", 
                    new { returnUrl = Url.Action("Address", "Checkout") });
            }

            return View(new Address());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAddress(Address address)
        {
            //if (ModelState.IsValid)
            //{
                try
                {
                    // Add user ID to address if user is authenticated
                    if (User.Identity.IsAuthenticated)
                    {
                        address.UserId = User.Identity.Name;
                    }

                    // Save address
                    await _addressRepository.InsertOneAsync(address);
                    _logger.LogInformation($"Shipping address saved successfully for {address.Email}");

                    // Store the address ID in TempData to reference it later
                    TempData["AddressId"] = address.Id;

                    // Redirect to credit card information page
                    return RedirectToAction("EnterCreditCard", "CreditCard");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while saving the address information.");
                    ModelState.AddModelError(string.Empty, "An error occurred while saving your address. Please try again.");
                }
            //}

            return View("Address", address);
        }
    }
}