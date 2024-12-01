using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using e_commerce.Controllers;
using e_commerce.Models;
using e_commerce.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Linq.Expressions;

namespace e_commerce.Tests.Controllers
{
    public class OrdersControllerTests
    {
        private readonly Mock<IMongoDBRepository<Order>> _mockOrderRepository;
        private readonly Mock<ILogger<OrdersController>> _mockLogger;
        private readonly OrdersController _controller;
        private const string TEST_USER_ID = "test-user-123";

        public OrdersControllerTests()
        {
            _mockOrderRepository = new Mock<IMongoDBRepository<Order>>();
            _mockLogger = new Mock<ILogger<OrdersController>>();
            _controller = new OrdersController(_mockOrderRepository.Object, _mockLogger.Object);
        }

        private void SetupUserContext(string userId = TEST_USER_ID, string role = "Customer")
        {
            var claims = new List<Claim>();
            
            if (!string.IsNullOrEmpty(role))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
            
            if (!string.IsNullOrEmpty(userId))
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
            }

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        [Fact]
        public async Task Index_ReturnsEmptyList_WhenUserIdIsNull()
        {
            // Arrange
            // Setup context without user ID, only role
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, "Customer")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            
            var httpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            };
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Order>>(viewResult.Model);
            Assert.Empty(model);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("UserId is null or empty")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task Index_ReturnsOrdersForUser_WhenUserHasOrders()
        {
            // Arrange
            SetupUserContext();

            var testOrders = new List<Order>
            {
                new Order 
                { 
                    Id = "1", 
                    UserId = TEST_USER_ID,
                    OrderDate = DateTime.Now,
                    Items = new List<OrderItem>
                    {
                        new OrderItem 
                        { 
                            ProductId = "prod1",
                            ProductName = "Test Product",
                            Quantity = 2,
                            UnitPrice = 10.99m,
                            TotalPrice = 21.98m
                        }
                    },
                    TotalAmount = 21.98m,
                    OrderStatus = "Completed"
                }
            };

            _mockOrderRepository.Setup(repo => repo.FilterByAsync(It.IsAny<Expression<Func<Order, bool>>>()))
                .ReturnsAsync(testOrders);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Order>>(viewResult.Model);
            Assert.Single(model);
            Assert.Equal(TEST_USER_ID, model.First().UserId);
        }

        [Fact]
        public async Task Index_ReturnsEmptyList_WhenUserHasNoOrders()
        {
            // Arrange
            SetupUserContext();

            _mockOrderRepository.Setup(repo => repo.FilterByAsync(It.IsAny<Expression<Func<Order, bool>>>()))
                .ReturnsAsync(new List<Order>());

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Order>>(viewResult.Model);
            Assert.Empty(model);
        }

        [Fact]
        public async Task Index_ReturnsEmptyList_WhenExceptionOccurs()
        {
            // Arrange
            SetupUserContext();

            _mockOrderRepository.Setup(repo => repo.FilterByAsync(It.IsAny<Expression<Func<Order, bool>>>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Order>>(viewResult.Model);
            Assert.Empty(model);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task Index_HandlesNullUser()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Order>>(viewResult.Model);
            Assert.Empty(model);
        }
    }
}