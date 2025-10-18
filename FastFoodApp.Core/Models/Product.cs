using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace FastFoodApp.Core.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Indica si este producto lleva control de stock.
        /// Si es false, el campo Stock no se usa.
        /// Ejemplo: Hamburguesas = false (se preparan), Gaseosas = true (tienen stock físico)
        /// </summary>
        public bool TracksStock { get; set; } = true;

        /// <summary>
        /// Cantidad en stock. Solo se usa si TracksStock = true
        /// </summary>
        public int? Stock { get; set; }

        /// <summary>
        /// Stock mínimo para alertas. Solo aplica si TracksStock = true
        /// </summary>
        public int? MinimumStock { get; set; }

        /// <summary>
        /// Stock máximo. Solo aplica si TracksStock = true
        /// </summary>
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

        /// <summary>
        /// Indica si el producto tiene receta (usa materias primas/ingredientes)
        /// </summary>
        public bool HasRecipe { get; set; } = false;

        /// <summary>
        /// Receta del producto: lista de materias primas/ingredientes que lo componen
        /// </summary>
        public virtual ICollection<ProductRecipe>? Recipe { get; set; }

        // Propiedad calculada para mostrar en la UI (no se guarda en BD)
        [NotMapped]
        public decimal DisplayPrice { get; set; }

        /// <summary>
        /// Obtiene el stock actual o null si no lleva control de stock
        /// </summary>
        [NotMapped]
        public int? StockActual => TracksStock ? Stock : null;
    }
}