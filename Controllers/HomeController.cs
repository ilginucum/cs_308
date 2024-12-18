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
        private readonly IMongoDBRepository<Order> _orderRepository; // Yeni eklendi
        private readonly IMongoDBRepository<Category> _categoryRepository;

        public HomeController(ILogger<HomeController> logger, IMongoDBRepository<Product> productRepository, IMongoDBRepository<Order> orderRepository, IMongoDBRepository<Category> categoryRepository) // Yeni ekleme
        {
            _logger = logger;
            _productRepository = productRepository;
            _orderRepository = orderRepository; // Yeni ekleme
            _categoryRepository = categoryRepository;
        }

         public async Task<IActionResult> Index()
    {
        var userRole = User.FindFirstValue(ClaimTypes.Role);
        ViewBag.UserRole = userRole;
        var products = await _productRepository.GetAllAsync();

        // Popülerlik hesaplama (Orders koleksiyonundan)
        var orders = await _orderRepository.GetAllAsync(); // Yeni ekleme
        var productPopularity = orders
            .SelectMany(o => o.Items) // Sipariş içindeki tüm ürünleri al
            .GroupBy(i => i.ProductId) // ProductId'ye göre grupla
            .Select(g => new { ProductId = g.Key, TotalQuantity = g.Sum(i => i.Quantity) }) // Toplam miktar hesapla
            .ToDictionary(p => p.ProductId, p => p.TotalQuantity); // Sözlüğe çevir (ProductId => Toplam)

        ViewBag.ProductPopularity = productPopularity; // ViewBag ile gönder (Yeni ekleme)

        // Kategorileri MongoDB'deki Categories koleksiyonundan al
        var categories = await _categoryRepository.GetAllAsync();
        ViewBag.Genres = categories.Select(c => c.Name).Distinct().OrderBy(g => g).ToList();
        

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