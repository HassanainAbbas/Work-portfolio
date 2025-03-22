using System;
using System.Collections.Generic;

namespace Organify.Models
{
    public class Order
    {
        public int Id { get; set; }              // Primary Key
        public int UserId { get; set; }          // Foreign Key (User)
        public User User { get; set; }           // Navigation Property
        public DateTime OrderDate { get; set; }  // Date of Order
        public decimal TotalAmount { get; set; } // Total Price of the Order
        public string ShippingAddress { get; set; } // Address for Delivery

        // Navigation Property for Order Items
        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
