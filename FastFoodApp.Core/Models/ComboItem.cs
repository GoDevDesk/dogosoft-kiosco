namespace FastFoodApp.Core.Models
{
    /// <summary>
    /// Representa un producto dentro de un combo
    /// </summary>
    public class ComboItem
    {
        public int Id { get; set; }
        public int ComboId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;

        /// <summary>
        /// Si es true, el cliente puede elegir entre productos alternativos
        /// Ejemplo: puede elegir Coca, Sprite o Fanta
        /// </summary>
        public bool AllowsSubstitution { get; set; } = false;

        /// <summary>
        /// Nombre del grupo de substitución (ej: "Bebida", "Hamburguesa")
        /// </summary>
        public string? SubstitutionGroup { get; set; }

        // Navegación
        public Combo Combo { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}