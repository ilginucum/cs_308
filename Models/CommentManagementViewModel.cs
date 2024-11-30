using System;

namespace e_commerce.Models
{
    public class CommentManagementViewModel
    {
        public string Id { get; set; }
        public string ProductId { get; set; }
        public string UserId { get; set; }
        public string ProductName { get; set; }
        public string UserName { get; set; }
        public string CommentText { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; }
        public bool HasPurchased { get; set; }
    }
}