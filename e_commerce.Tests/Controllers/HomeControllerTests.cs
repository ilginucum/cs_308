using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using e_commerce.Controllers;
using e_commerce.Models;
using e_commerce.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Security.Principal;

namespace e_commerce.Tests.Controllers
{
    public class HomeControllerTests
    {
        private readonly Mock<ILogger<HomeController>> _mockLogger;
        private readonly Mock<IMongoDBRepository<Product>> _mockProductRepository;
        private readonly Mock<IMongoDBRepository<Order>> _mockOrderRepository;
        private readonly HomeController _controller;

        public HomeControllerTests()
        {
            _mockLogger = new Mock<ILogger<HomeController>>();
            _mockProductRepository = new Mock<IMongoDBRepository<Product>>();
            _mockOrderRepository = new Mock<IMongoDBRepository<Order>>();
            _controller = new HomeController(
                _mockLogger.Object,
                _mockProductRepository.Object,
                _mockOrderRepository.Object
            );

            // Set up default user context in constructor
            SetupUserContext();
        }

        private void SetupUserContext(string role = "Customer")
        {
            // Create claims identity
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            // Create and setup mock HTTP context
            var httpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            };

            // Set controller context
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        [Fact]
        public async Task Index_ReturnsViewWithProducts()
        {
            // Arrange
            var products = new List<Product>
            {
                new Product { Id = "1", Name = "Book 1", Author = "Author 1", Genre = "Fiction", Price = 10.99m },
                new Product { Id = "2", Name = "Book 2", Author = "Author 2", Genre = "Non-Fiction", Price = 15.99m },
                new Product { Id = "3", Name = "Book 3", Genre = "Fiction", Price = 12.99m } // Missing author
            };

            var orders = new List<Order>
            {
                new Order
                {
                    Id = "1",
                    OrderDate = DateTime.Now,
                    OrderStatus = "Completed",
                    Items = new List<OrderItem>
                    {
                        new OrderItem 
                        { 
                            ProductId = "1",
                            ProductName = "Book 1",
                            Quantity = 2,
                            UnitPrice = 10.99m,
                            TotalPrice = 21.98m
                        },
                        new OrderItem 
                        { 
                            ProductId = "2",
                            ProductName = "Book 2",
                            Quantity = 1,
                            UnitPrice = 15.99m,
                            TotalPrice = 15.99m
                        }
                    },
                    TotalAmount = 37.97m
                }
            };

            _mockProductRepository.Setup(repo => repo.GetAllAsync())
                .ReturnsAsync(products);
            
            _mockOrderRepository.Setup(repo => repo.GetAllAsync())
                .ReturnsAsync(orders);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Product>>(viewResult.Model);
            
            Assert.Equal(3, model.Count());
            Assert.Equal("Unknown Author", model.First(p => p.Id == "3").Author);
            
            Assert.Equal("Customer", viewResult.ViewData["UserRole"]);
            
            var genres = Assert.IsType<List<string>>(viewResult.ViewData["Genres"]);
            Assert.Equal(2, genres.Count);
            Assert.Contains("Fiction", genres);
            Assert.Contains("Non-Fiction", genres);

            var popularity = Assert.IsType<Dictionary<string, int>>(viewResult.ViewData["ProductPopularity"]);
            Assert.Equal(2, popularity["1"]); // First product ordered twice
            Assert.Equal(1, popularity["2"]); // Second product ordered once
        }

        [Fact]
        public async Task Index_HandlesEmptyProducts()
        {
            // Arrange
            _mockProductRepository.Setup(repo => repo.GetAllAsync())
                .ReturnsAsync(new List<Product>());
            
            _mockOrderRepository.Setup(repo => repo.GetAllAsync())
                .ReturnsAsync(new List<Order>());

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Product>>(viewResult.Model);
            Assert.Empty(model);
            
            var genres = Assert.IsType<List<string>>(viewResult.ViewData["Genres"]);
            Assert.Empty(genres);

            var popularity = Assert.IsType<Dictionary<string, int>>(viewResult.ViewData["ProductPopularity"]);
            Assert.Empty(popularity);
        }

        [Fact]
        public async Task Index_HandlesNullUserRole()
        {
            // Arrange
            var identity = new ClaimsIdentity(new List<Claim>(), "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            };
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            _mockProductRepository.Setup(repo => repo.GetAllAsync())
                .ReturnsAsync(new List<Product>());
            
            _mockOrderRepository.Setup(repo => repo.GetAllAsync())
                .ReturnsAsync(new List<Order>());

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewData["UserRole"]);
        }

        [Fact]
        public async Task Index_CalculatesProductPopularityCorrectly()
        {
            // Arrange
            var products = new List<Product>
            {
                new Product { Id = "1", Name = "Book 1", Price = 10.99m },
                new Product { Id = "2", Name = "Book 2", Price = 15.99m }
            };

            var orders = new List<Order>
            {
                new Order
                {
                    Id = "1",
                    OrderDate = DateTime.Now,
                    OrderStatus = "Completed",
                    Items = new List<OrderItem>
                    {
                        new OrderItem 
                        { 
                            ProductId = "1",
                            ProductName = "Book 1",
                            Quantity = 3,
                            UnitPrice = 10.99m,
                            TotalPrice = 32.97m
                        },
                        new OrderItem 
                        { 
                            ProductId = "2",
                            ProductName = "Book 2",
                            Quantity = 1,
                            UnitPrice = 15.99m,
                            TotalPrice = 15.99m
                        }
                    },
                    TotalAmount = 48.96m
                },
                new Order
                {
                    Id = "2",
                    OrderDate = DateTime.Now,
                    OrderStatus = "Completed",
                    Items = new List<OrderItem>
                    {
                        new OrderItem 
                        { 
                            ProductId = "1",
                            ProductName = "Book 1",
                            Quantity = 2,
                            UnitPrice = 10.99m,
                            TotalPrice = 21.98m
                        }
                    },
                    TotalAmount = 21.98m
                }
            };

            _mockProductRepository.Setup(repo => repo.GetAllAsync())
                .ReturnsAsync(products);
            
            _mockOrderRepository.Setup(repo => repo.GetAllAsync())
                .ReturnsAsync(orders);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var popularity = Assert.IsType<Dictionary<string, int>>(viewResult.ViewData["ProductPopularity"]);
            
            Assert.Equal(5, popularity["1"]); // Total quantity should be 3 + 2 = 5
            Assert.Equal(1, popularity["2"]); // Total quantity should be 1
        }

        [Fact]
        public void Privacy_ReturnsView()
        {
            // Act
            var result = _controller.Privacy();

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Error_ReturnsViewWithErrorViewModel()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = httpContext
            };

            // Act
            var result = _controller.Error();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ErrorViewModel>(viewResult.Model);
            Assert.NotNull(model.RequestId);
        }

    

        [Fact]
        public async Task Index_HandlesNullGenres()
        {
            // Arrange
            var products = new List<Product>
            {
                new Product { Id = "1", Name = "Book 1", Genre = null },
                new Product { Id = "2", Name = "Book 2", Genre = "" },
                new Product { Id = "3", Name = "Book 3", Genre = "Fiction" }
            };

            _mockProductRepository.Setup(repo => repo.GetAllAsync())
                .ReturnsAsync(products);
            
            _mockOrderRepository.Setup(repo => repo.GetAllAsync())
                .ReturnsAsync(new List<Order>());

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var genres = Assert.IsType<List<string>>(viewResult.ViewData["Genres"]);
            Assert.Single(genres);
            Assert.Contains("Fiction", genres);
        }

        [Fact]
        public async Task Index_HandlesMultipleGenresAndProducts()
        {
            // Arrange
            var products = new List<Product>
            {
                new Product { Id = "1", Name = "Book 1", Author = "Author 1", Genre = "Fiction", Price = 10.99m },
                new Product { Id = "2", Name = "Book 2", Author = "Author 2", Genre = "Non-Fiction", Price = 15.99m },
                new Product { Id = "3", Name = "Book 3", Author = "Author 3", Genre = "Mystery", Price = 12.99m },
                new Product { Id = "4", Name = "Book 4", Author = "Author 4", Genre = "Fiction", Price = 9.99m },
                new Product { Id = "5", Name = "Book 5", Author = null, Genre = "Mystery", Price = 11.99m }
            };

            var orders = new List<Order>
            {
                new Order
                {
                    Id = "1",
                    OrderDate = DateTime.Now,
                    OrderStatus = "Completed",
                    Items = new List<OrderItem>
                    {
                        new OrderItem 
                        { 
                            ProductId = "1",
                            ProductName = "Book 1",
                            Quantity = 2,
                            UnitPrice = 10.99m,
                            TotalPrice = 21.98m
                        },
                        new OrderItem 
                        { 
                            ProductId = "3",
                            ProductName = "Book 3",
                            Quantity = 3,
                            UnitPrice = 12.99m,
                            TotalPrice = 38.97m
                        }
                    },
                    TotalAmount = 60.95m
                }
            };

            _mockProductRepository.Setup(repo => repo.GetAllAsync())
                .ReturnsAsync(products);
            
            _mockOrderRepository.Setup(repo => repo.GetAllAsync())
                .ReturnsAsync(orders);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Product>>(viewResult.Model);
            
            // Check total number of products
            Assert.Equal(5, model.Count());
            
            // Verify unknown author is set
            Assert.Equal("Unknown Author", model.First(p => p.Id == "5").Author);
            
            // Check genres
            var genres = Assert.IsType<List<string>>(viewResult.ViewData["Genres"]);
            Assert.Equal(3, genres.Count);
            Assert.Contains("Fiction", genres);
            Assert.Contains("Non-Fiction", genres);
            Assert.Contains("Mystery", genres);
            
            // Check product popularity
            var popularity = Assert.IsType<Dictionary<string, int>>(viewResult.ViewData["ProductPopularity"]);
            Assert.Equal(2, popularity["1"]); // Book 1 ordered twice
            Assert.Equal(3, popularity["3"]); // Book 3 ordered three times
            Assert.False(popularity.ContainsKey("2")); // Book 2 not ordered
            Assert.False(popularity.ContainsKey("4")); // Book 4 not ordered
            Assert.False(popularity.ContainsKey("5")); // Book 5 not ordered
        }
        
    }
}