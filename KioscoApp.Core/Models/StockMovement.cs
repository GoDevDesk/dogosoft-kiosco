using KioscoApp.Core.Models;

public class StockMovement
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int? SupplierId { get; set; } // Si es una compra
    public string MovementType { get; set; } // Compra, Venta, Ajuste+, Ajuste-, etc
    public int Quantity { get; set; } // Positivo o negativo
    public decimal? Cost { get; set; } // Costo unitario si es compra
    public string Reason { get; set; } // Motivo del movimiento
    public DateTime Date { get; set; }
    public string User { get; set; }

    // Navigation
    public Product Product { get; set; }
    public Supplier? Supplier { get; set; }
}