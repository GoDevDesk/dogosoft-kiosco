namespace KioscoApp.Core.Models
{
    public class PriceList
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public bool IsActive { get; set; }
        public bool IsProtected { get; set; } = false;
        public DateTime CreatedAt { get; set; }
    }
}