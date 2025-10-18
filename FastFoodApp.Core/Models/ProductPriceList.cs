namespace FastFoodApp.Core.Models
{
    public class ProductPriceList
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int PriceListId { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal ProfitPercentage { get; set; }
        public decimal InternalTaxPercentage { get; set; }
        public string IVA { get; set; } = "21%";
        public DateTime LastUpdate { get; set; }

        // Navigation properties
        public Product Product { get; set; } = null!;
        public PriceList PriceList { get; set; } = null!;
    }
}
