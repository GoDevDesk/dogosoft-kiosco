using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FastFoodApp.Core.Models
{
    /// <summary>
    /// Representa la receta de un producto: qué materias primas (ingredientes) usa y en qué cantidad
    /// SOLO incluye materias primas donde IsIngredient = true
    /// </summary>
    [Table("ProductRecipes")]
    public class ProductRecipe
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// ID del producto que usa esta materia prima
        /// </summary>
        [Required]
        public int ProductId { get; set; }

        /// <summary>
        /// ID de la materia prima usada (debe ser IsIngredient = true)
        /// </summary>
        [Required]
        public int RawMaterialId { get; set; }

        /// <summary>
        /// Cantidad de la materia prima necesaria para preparar 1 unidad del producto
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,4)")]
        public decimal Quantity { get; set; }

        /// <summary>
        /// Fecha de creación del registro
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Fecha de última actualización
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        [ForeignKey("RawMaterialId")]
        public virtual RawMaterial? RawMaterial { get; set; }
    }
}