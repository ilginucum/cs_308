using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using e_commerce.Controllers;
using e_commerce.Models;
using e_commerce.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace e_commerce.Tests.Controllers
{
    public class SalesControllerTests
    {
        private readonly Mock<IMongoDBRepository<Product>> _mockProductRepository;
        private readonly Mock<ILogger<SalesController>> _mockLogger;
        private readonly SalesController _controller;

        public SalesControllerTests()
        {
            _mockProductRepository = new Mock<IMongoDBRepository<Product>>();
            _mockLogger = new Mock<ILogger<SalesController>>();
            _controller = new SalesController(_mockProductRepository.Object, _mockLogger.Object);
        }


        [Fact]
        public async Task Index_ReturnsEmptyList_WhenNoProductsExist()
        {
            // Arrange
            _mockProductRepository.Setup(repo => repo.GetAllAsync())
                .ReturnsAsync((List<Product>)null);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Product>>(viewResult.Model);
            Assert.Empty(model);
        }

        [Fact]
        public async Task Index_ReturnsEmptyList_WhenExceptionOccurs()
        {
            // Arrange
            _mockProductRepository.Setup(repo => repo.GetAllAsync())
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Product>>(viewResult.Model);
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
        public async Task SetPrice_Get_ReturnsNotFound_WhenIdIsNull()
        {
            // Arrange & Act
            var result = await _controller.SetPrice(null);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task SetPrice_Get_ReturnsNotFound_WhenProductNotFound()
        {
            // Arrange
            string testId = "nonexistent";
            _mockProductRepository.Setup(repo => repo.FindByIdAsync(testId))
                .ReturnsAsync((Product)null);

            // Act
            var result = await _controller.SetPrice(testId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task SetPrice_Get_ReturnsViewWithProduct_WhenProductExists()
        {
            // Arrange
            string testId = "1";
            var product = new Product { Id = testId, Name = "Test Product", Price = 10.99m };
            _mockProductRepository.Setup(repo => repo.FindByIdAsync(testId))
                .ReturnsAsync(product);

            // Act
            var result = await _controller.SetPrice(testId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Product>(viewResult.Model);
            Assert.Equal(testId, model.Id);
        }

        [Fact]
        public async Task SetPrice_Post_ReturnsNotFound_WhenProductNotFound()
        {
            // Arrange
            string testId = "nonexistent";
            decimal testPrice = 15.99m;
            _mockProductRepository.Setup(repo => repo.FindByIdAsync(testId))
                .ReturnsAsync((Product)null);

            // Act
            var result = await _controller.SetPrice(testId, testPrice);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }


        [Fact]
        public async Task SetPrice_Post_ReturnsViewWithError_WhenExceptionOccurs()
        {
            // Arrange
            string testId = "1";
            decimal testPrice = 15.99m;
            var product = new Product { Id = testId, Name = "Test Product", Price = 10.99m };
            _mockProductRepository.Setup(repo => repo.FindByIdAsync(testId))
                .ReturnsAsync(product);
            _mockProductRepository.Setup(repo => repo.ReplaceOneAsync(It.IsAny<Product>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.SetPrice(testId, testPrice);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<Product>(viewResult.Model);
            Assert.False(_controller.ModelState.IsValid);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }
    }
}