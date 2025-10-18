namespace FastFoodApp.Core.Models
{
    /// <summary>
    /// Representa un combo (paquete de productos con precio especial)
    /// </summary>
    public class Combo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsActive { get; set; } = true;
        public int? CategoryId { get; set; }

        // Navegación
        public Category? Category { get; set; }
        public List<ComboItem> Items { get; set; } = new();
    }
}