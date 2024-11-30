using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace e_commerce.Models{

    public class OrderManagementViewModel
{
    public List<Order> ProcessingOrders { get; set; }
    public List<Order> InTransitOrders { get; set; }
    public List<Order> DeliveredOrders { get; set; }
    public Dictionary<string, Address> Addresses { get; set; }
}
}