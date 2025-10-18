// OrderItem.cs
namespace FastFoodApp.Core.Models
{
    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int? ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get; set; }
        public string? Customizations { get; set; } // "sin cebolla;extra queso"

        // Navegación
        public Order Order { get; set; } = null!;
        public Product? Product { get; set; } = null!;
    }
}