using System.Collections.Generic;


namespace e_commerce.Models
{
    public class SalesViewModel
    {
        public IEnumerable<Product> Products { get; set; }
        public IEnumerable<Order> Orders { get; set; }
        public IEnumerable<Order> FilteredOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal ProfitLoss { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public IEnumerable<Order> RefundRequests { get; set; }
        public IEnumerable<Order>RefundStatus { get; set; }


    }
}
