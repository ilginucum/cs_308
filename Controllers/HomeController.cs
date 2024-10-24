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

        // Get unique genres from all products
        var allGenres = products
            .Where(p => !string.IsNullOrEmpty(p.Genre))
            .Select(p => p.Genre)
            .Distinct()
            .OrderBy(g => g)
            .ToList();
        
        ViewBag.Genres = allGenres;

        foreach (var product in products)
        {
            if (string.IsNullOrEmpty(product.Author))
            {
                product.Author = "Unknown Author";
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