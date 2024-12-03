using Microsoft.AspNetCore.Mvc;
using e_commerce.Models;
using System.Collections.Generic;

namespace e_commerce.Controllers
{
    public class WishlistController : Controller
    {
        // Temporary mock data for testing
        private static List<WishlistItem> wishlist = new List<WishlistItem>();

        public IActionResult Index()
        {
            return View(wishlist); // Pass the wishlist items to the view
        }

        [HttpPost]
        public IActionResult AddToWishlist(int productId, string productName, string author, decimal price, string imageUrl)
        {
            wishlist.Add(new WishlistItem
            {
                ProductId = productId,
                ProductName = productName,
                Author = author,
                Price = price,
                ImageUrl = imageUrl
            });

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult RemoveFromWishlist(int productId)
        {
            wishlist.RemoveAll(item => item.ProductId == productId);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult MoveToCart(int productId)
        {
            var item = wishlist.Find(w => w.ProductId == productId);
            if (item != null)
            {
                // Code to add the item to the cart
                wishlist.Remove(item);
            }
            return RedirectToAction("Index");
        }
    }
}
