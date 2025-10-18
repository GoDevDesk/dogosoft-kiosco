using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FastFoodApp.Core.Models
{
    /// <summary>
    /// Representa una materia prima/insumo que puede o no ser ingrediente
    /// </summary>
    [Table("RawMaterials")]
    public class RawMaterial
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Código único del insumo
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Nombre del insumo
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Descripción o notas
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Indica si este insumo ES un ingrediente (se usa en recetas)
        /// true = Ingrediente (ej: carne, lechuga) - se descuenta en ventas
        /// false = Insumo no ingrediente (ej: papel, vasos) - solo para compras
        /// </summary>
        public bool IsIngredient { get; set; } = false;

        /// <summary>
        /// Unidad de medida (KG, L, UN, GR, ML, etc.)
        /// </summary>
        [Required]
        [MaxLength(10)]
        public string Unit { get; set; } = "UN";

        /// <summary>
        /// Cantidad disponible actual (solo significativo si IsIngredient = true)
        /// </summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal AvailableQuantity { get; set; } = 0;

        /// <summary>
        /// Cantidad mínima (para alertas, solo si IsIngredient = true)
        /// </summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal? MinimumQuantity { get; set; }

        /// <summary>
        /// Cantidad máxima (para alertas, solo si IsIngredient = true)
        /// </summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal? MaximumQuantity { get; set; }

        /// <summary>
        /// Costo por unidad (puede actualizarse con cada compra)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? UnitCost { get; set; }

        /// <summary>
        /// Categoría del insumo (Carnes, Vegetales, Lácteos, Packaging, etc.)
        /// </summary>
        public int? CategoryId { get; set; }

        /// <summary>
        /// Proveedor principal
        /// </summary>
        public int? SupplierId { get; set; }

        /// <summary>
        /// Activo/Inactivo
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Fecha de creación
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Fecha de última actualización
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        [ForeignKey("SupplierId")]
        public virtual Supplier? Supplier { get; set; }
    }
}