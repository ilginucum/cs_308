using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace e_commerce.Models
{
    public class CartItem
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Required]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ProductId { get; set; }


        [Required]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ShoppingCartId { get; set; }

        
        [Required]
        public string ProductName { get; set; }

        [Required]
        public int QuantityInCart { get; set; }


        [Required]
        public decimal UnitPrice { get; set; }

        public decimal TotalPrice => QuantityInCart * UnitPrice;

        // ShoppingCart referansını kaldırdık çünkü bu, döngüsel referansa yol açabilir
    }
}