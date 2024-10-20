using Microsoft.AspNetCore.Mvc;
using e_commerce.Models;
using e_commerce.Data;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace e_commerce.Controllers
{
    public class CreditCardController : Controller
    {
        private readonly IMongoDBRepository<CreditCard> _creditCardRepository;
        private readonly ILogger<CreditCardController> _logger;

        public CreditCardController(
            IMongoDBRepository<CreditCard> creditCardRepository,
            ILogger<CreditCardController> logger)
        {
            _creditCardRepository = creditCardRepository;
            _logger = logger;

            _logger.LogInformation("CreditCardController initialized");
        }

        [HttpGet]
        public IActionResult EnterCreditCard()
        {
            return View(new CreditCard());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveCreditCard(CreditCard creditCard)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _creditCardRepository.InsertOneAsync(creditCard);
                    _logger.LogInformation("Credit card information saved successfully.");
                    return RedirectToAction("Confirmation");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while saving the credit card information.");
                    ModelState.AddModelError(string.Empty, "An error occurred while saving the credit card information. Please try again.");
                }
            }

            return View("EnterCreditCard", creditCard);
        }

        public IActionResult Confirmation()
        {
            return View();
        }
    }
}
