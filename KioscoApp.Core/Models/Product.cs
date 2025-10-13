using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace KioscoApp.Core.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Stock { get; set; }
        public int? MinimumStock { get; set; }
        public int? MaximumStock { get; set; }

        // Relación con Category
        public int? CategoryId { get; set; }
        public Category? Category { get; set; }

        public string UnitOfSale { get; set; } = "UN";
        public string? ImagePath { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Observations { get; set; }
        public string? ShortDescription { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastPriceUpdate { get; set; }

        public bool HasExpiry { get; set; } = false;
        public DateTime? ExpiryDate { get; set; }
        public int? ExpiryAlertDays { get; set; }

        // Propiedad calculada para mostrar en la UI (no se guarda en BD)
        [NotMapped]
        public decimal DisplayPrice { get; set; }
    }
}