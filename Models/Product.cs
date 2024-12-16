using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace e_commerce.Models
{
    public class Product
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Name { get; set; }
        //model yerine
        public string Author { get; set; }
        public string Model { get; set; }
        public string SerialNumber { get; set; }
        public string Description { get; set; }
        public int QuantityInStock { get; set; }
        public decimal Price { get; set; }
        public bool WarrantyStatus { get; set; }
        public string DistributorInformation { get; set; }
        public string Genre { get; set; }
        //public string CategoryId { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal DiscountedPrice { get; set; }


    }
}