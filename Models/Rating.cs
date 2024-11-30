using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace e_commerce.Models
{
    public class Rating
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } // MongoDB'deki benzersiz kimlik alanı

        [BsonElement("ProductId")]
        public string ProductId { get; set; } // Puanın hangi ürüne ait olduğunu belirtir

        [BsonElement("UserId")]
        public string UserId { get; set; } // Puanı veren kullanıcının kimliğini belirtir

        [BsonElement("UserName")]
        public string UserName { get; set; } // Puanı veren kullanıcının kullanıcı adı

        [BsonElement("Score")]
        public int Score { get; set; } // Kullanıcının verdiği puan (1 ile 5 arasında)

        [BsonElement("CreatedAt")]
        public DateTime CreatedAt { get; set; } // Puanın verildiği tarih ve saat
    }
}
