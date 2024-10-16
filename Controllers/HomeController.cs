using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using e_commerce.Models;
using System.Security.Claims;
using e_commerce.Data;

namespace e_commerce.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IMongoDBRepository<Product> _productRepository;

        public HomeController(ILogger<HomeController> logger, IMongoDBRepository<Product> productRepository)
        {
            _logger = logger;
            _productRepository = productRepository;
        }

        public async Task<IActionResult> Index()
        {
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            ViewBag.UserRole = userRole;
            var products = await _productRepository.GetAllAsync();
            
            // Assuming DistributorInformation is used for the author name
            // If not, replace it with the appropriate field
            foreach (var product in products)
            {
                if (string.IsNullOrEmpty(product.DistributorInformation))
                {
                    product.DistributorInformation = "Unknown Author";
                }
            }
            
            return View(products);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}