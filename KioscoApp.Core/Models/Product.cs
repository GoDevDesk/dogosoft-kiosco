using System;

namespace KioscoApp.Core.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Code { get; set; }           // codigo de barra opcional
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string Category { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string ImagePath { get; set; } 

    }
}
