using System;
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
        public IEnumerable<Order> RefundStatus { get; set; }

        // Adding monthly financial data
        public List<MonthlyData> MonthlyFinancialData { get; set; } = new List<MonthlyData>();

        // Nested class for monthly financial data
        public class MonthlyData
        {
            public DateTime Month { get; set; }
            public decimal Revenue { get; set; }
            public decimal Cost { get; set; }
            public decimal Profit { get; set; }
        }
    }
}