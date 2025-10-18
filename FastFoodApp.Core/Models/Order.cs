// Order.cs
namespace FastFoodApp.Core.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int OrderNumber { get; set; }
        public DateTime Date { get; set; }
        public string CustomerName { get; set; } = "Cliente";
        public string? CustomerPhone { get; set; }
        public string Status { get; set; } = "Pendiente"; // Pendiente, En Preparación, Listo, Entregado
        public decimal Total { get; set; }
        public DateTime? CompletedAt { get; set; }

        // Navegación
        public List<OrderItem> Items { get; set; } = new();
    }
}