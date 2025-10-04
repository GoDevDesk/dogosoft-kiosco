using System;

namespace KioscoApp.Core.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string? Category { get; set; } 
        public DateTime? ExpiryDate { get; set; }
        public string? ImagePath { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Observations { get; set; }
    }
}