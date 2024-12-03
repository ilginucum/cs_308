using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace e_commerce.Models
{
    public class WishlistItem
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string ProductId { get; set; }
        public string UserId { get; set; }
        public string ProductName { get; set; }
        public string Author { get; set; }
        public decimal Price { get; set; }
        public string ImageUrl { get; set; }
        public class WishlistViewModel
        {
            public WishlistItem WishlistItem { get; set; }
            public int QuantityInStock { get; set; }
        }
    }
}
