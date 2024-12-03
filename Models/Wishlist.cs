namespace e_commerce.Models
{
    public class WishlistItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string Author { get; set; }
        public decimal Price { get; set; }
        public string ImageUrl { get; set; }
    }
}
