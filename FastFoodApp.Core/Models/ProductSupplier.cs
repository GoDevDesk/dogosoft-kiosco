namespace FastFoodApp.Core.Models
{
    public class ProductSupplier
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int SupplierId { get; set; }
        public decimal Cost { get; set; }
        public bool IsDefault { get; set; } = false;
        public DateTime LastUpdate { get; set; } = DateTime.Now;

        // Navigation properties
        public Product Product { get; set; } = null!;
        public Supplier Supplier { get; set; } = null!;
    }
}