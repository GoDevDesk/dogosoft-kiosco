using System;
using System.Collections.Generic;

namespace FastFoodApp.Core.Models
{
    public class Sale
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public decimal Total { get; set; }
        public string SaleType { get; set; } // "Interna" o mas tarde "Fiscal"
        public string User { get; set; }
        public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
    }

    public class SaleItem
    {
        public int Id { get; set; }
        public int SaleId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get; set; }

        public Product Product { get; set; }
        public Sale Sale { get; set; }
    }
}
