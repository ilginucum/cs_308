using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using e_commerce.Controllers;
using e_commerce.Models;
using e_commerce.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace e_commerce.Tests.Controllers
{
    public class CheckoutControllerTests
    {
        private readonly Mock<IMongoDBRepository<Address>> _mockAddressRepository;
        private readonly Mock<IMongoDBRepository<ShoppingCart>> _mockCartRepository;
        private readonly Mock<IMongoDBRepository<ProfileAddress>> _mockProfileAddressRepository;
        private readonly Mock<ILogger<CheckoutController>> _mockLogger;
        private readonly CheckoutController _controller;

        public CheckoutControllerTests()
        {
            // Setup mocks
            _mockAddressRepository = new Mock<IMongoDBRepository<Address>>();
            _mockCartRepository = new Mock<IMongoDBRepository<ShoppingCart>>();
            _mockProfileAddressRepository = new Mock<IMongoDBRepository<ProfileAddress>>();
            _mockLogger = new Mock<ILogger<CheckoutController>>();

            // Create controller instance with mocked dependencies
            _controller = new CheckoutController(
                _mockAddressRepository.Object,
                _mockCartRepository.Object,
                _mockProfileAddressRepository.Object,
                _mockLogger.Object
            );

            // Setup mock user identity
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Name, "testUser@example.com")
            }, "mock"));

            // Set up controller context
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task GetSavedAddresses_ReturnsJsonResult_WithAddresses()
        {
            // Arrange
            var expectedAddresses = new List<ProfileAddress>
            {
                new ProfileAddress { 
                    UserId = "testUserId",
                    StreetAddress = "123 Test St",
                    City = "TestCity",
                    State = "TS",
                    ZipCode = "12345"
                },
                new ProfileAddress { 
                    UserId = "testUserId",
                    StreetAddress = "456 Test Ave",
                    City = "TestCity2",
                    State = "TS",
                    ZipCode = "12346"
                }
            };

            _mockProfileAddressRepository.Setup(repo => 
                repo.FilterByAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ProfileAddress, bool>>>()))
                .ReturnsAsync(expectedAddresses);

            // Act
            var result = await _controller.GetSavedAddresses();

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var addresses = Assert.IsType<List<ProfileAddress>>(jsonResult.Value);
            Assert.Equal(2, addresses.Count);
        }

        [Fact]
        public async Task Address_AuthenticatedUser_ReturnsViewWithAddresses()
        {
            // Arrange
            var savedAddresses = new List<ProfileAddress>
            {
                new ProfileAddress { 
                    UserId = "testUserId",
                    StreetAddress = "123 Test St",
                    City = "TestCity",
                    State = "TS",
                    ZipCode = "12345"
                }
            };

            _mockProfileAddressRepository.Setup(repo => 
                repo.FilterByAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ProfileAddress, bool>>>()))
                .ReturnsAsync(savedAddresses);

            // Act
            var result = await _controller.Address();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<Address>(viewResult.Model);
            var viewBagAddresses = Assert.IsType<List<ProfileAddress>>(viewResult.ViewData["SavedAddresses"]);
            Assert.Single(viewBagAddresses);
        }

        
        [Fact]
        public async Task SaveAddress_WhenExceptionOccurs_ReturnsViewWithError()
        {
            // Arrange
            var address = new Address
            {
                FullName = "Test User",
                Email = "test@example.com",
                StreetAddress = "123 Test St",
                City = "TestCity",
                State = "TS",
                ZipCode = "12345",
                PhoneNumber = "1234567890"
            };

            _mockAddressRepository.Setup(repo => 
                repo.InsertOneAsync(It.IsAny<Address>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.SaveAddress(address);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Address", viewResult.ViewName);
            Assert.Same(address, viewResult.Model);
            Assert.True(_controller.ModelState.ErrorCount > 0);
        }

        [Fact]
        public async Task SaveAddress_InvalidModelState_ReturnsViewWithModel()
        {
            // Arrange
            var address = new Address(); // Empty address will fail validation
            _controller.ModelState.AddModelError("Email", "Email is required");

            // Act
            var result = await _controller.SaveAddress(address);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Address", viewResult.ViewName);
            Assert.Same(address, viewResult.Model);
        }
    }
}