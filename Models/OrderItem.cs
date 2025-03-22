namespace Organify.Models
{
    public class OrderItem
    {
        public int Id { get; set; }              // Primary Key
        public int OrderId { get; set; }         // Foreign Key (Order)
        public int ProductId { get; set; }       // Foreign Key (Product)
        public int Quantity { get; set; }        // Quantity Ordered
        public decimal Price { get; set; }       // Price of the Product

        // Navigation Properties
        public Order Order { get; set; }
        public Product Product { get; set; }
    }
}
