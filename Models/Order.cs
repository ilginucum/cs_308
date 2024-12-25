using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace e_commerce.Models
{
    [BsonIgnoreExtraElements]
    public class Order
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]

        public string Id { get; set; }

        public string UserId { get; set; }
        public string AddressId { get; set; }
        public string CreditCardId { get; set; }
        public List<OrderItem> Items { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime OrderDate { get; set; }
        public string OrderStatus { get; set; }
        public string DeliveryId { get; set; }

        [BsonElement("RefundRequested")]
        public bool RefundRequested { get; set; }

        [BsonElement("RefundApproved")]
        public bool RefundApproved { get; set; }

        [BsonElement("RefundStatus")]
        public string RefundStatus { get; set; }

        [BsonElement("RefundRejected")]
        public bool RefundRejected { get; set; }


    }

    public class OrderItem
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public bool RefundRequested { get; set; } = false;

        [BsonElement("RefundApproved")]
        public bool RefundApproved { get; set; }

        [BsonElement("RefundStatus")]
        public string RefundStatus { get; set; }
        [BsonElement("RefundRejected")]
        public bool RefundRejected { get; set; }
    }
}