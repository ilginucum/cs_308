using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace e_commerce.Models
{
    public class Category
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; } // Nullable olarak tanımlandı

        [BsonRequired]
        public string Name { get; set; } = string.Empty; // Default değer ekle
    }
}
