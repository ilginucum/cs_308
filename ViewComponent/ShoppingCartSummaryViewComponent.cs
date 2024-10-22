using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using e_commerce.Data;
using e_commerce.Models;

namespace e_commerce.ViewComponents
{
    public class ShoppingCartSummaryViewComponent : ViewComponent
    {
        private readonly IMongoDBRepository<ShoppingCart> _shoppingCartRepository;

        public ShoppingCartSummaryViewComponent(IMongoDBRepository<ShoppingCart> shoppingCartRepository)
        {
            _shoppingCartRepository = shoppingCartRepository;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return View(0);
            }

            var cart = await _shoppingCartRepository.FindOneAsync(c => c.UserId == userId);
            int itemCount = cart?.Items.Sum(i => i.QuantityInCart) ?? 0;

            return View(itemCount);
        }
    }
}