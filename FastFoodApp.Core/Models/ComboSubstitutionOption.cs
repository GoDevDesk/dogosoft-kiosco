namespace FastFoodApp.Core.Models
{
    /// <summary>
    /// Productos alternativos para un item de combo
    /// Ejemplo: En vez de Coca, puede elegir Sprite
    /// </summary>
    public class ComboSubstitutionOption
    {
        public int Id { get; set; }
        public int ComboItemId { get; set; }
        public int AlternativeProductId { get; set; }

        // Navegación
        public ComboItem ComboItem { get; set; } = null!;
        public Product AlternativeProduct { get; set; } = null!;
    }
}