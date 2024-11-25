using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace e_commerce.Models
{
    public class ProductComment
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string ProductId { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string UserId { get; set; }

        public string ProductName { get; set; }

        public string UserName { get; set; }
        public string CommentText { get; set; }
        public DateTime CreatedAt { get; set; }

        public string Status { get; set; } = "pending"; // Varsayılan olarak "pending"
    }
}