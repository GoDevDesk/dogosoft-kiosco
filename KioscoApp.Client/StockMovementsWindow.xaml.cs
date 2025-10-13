using KioscoApp.Core.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Windows;

namespace KioscoApp.Client
{
    public class StockMovementViewModel
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string ProductName { get; set; } = "";
        public string MovementType { get; set; } = "";
        public int Quantity { get; set; }
        public decimal? Cost { get; set; }
        public string? SupplierName { get; set; }
        public string Reason { get; set; } = "";
        public string User { get; set; } = "";
    }

    public partial class StockMovementsWindow : Window
    {
        public StockMovementsWindow()
        {
            InitializeComponent();
            DpFechaDesde.SelectedDate = DateTime.Now.AddMonths(-1);
            CargarMovimientos();
        }

        private void CargarMovimientos()
        {
            using var ctx = new AppDbContext();

            var query = ctx.StockMovements
                .Include(sm => sm.Product)
                .Include(sm => sm.Supplier)
                .AsQueryable();

            // Filtro por producto
            if (TxtBuscarProducto != null && !string.IsNullOrWhiteSpace(TxtBuscarProducto.Text))
            {
                var filtro = TxtBuscarProducto.Text.ToLower();
                query = query.Where(sm => sm.Product.Name.ToLower().Contains(filtro) ||
                                         sm.Product.Code.ToLower().Contains(filtro));
            }

            // Filtro por tipo
            if (CmbTipoMovimiento != null && CmbTipoMovimiento.SelectedIndex > 0)
            {
                var tipo = ((System.Windows.Controls.ComboBoxItem)CmbTipoMovimiento.SelectedItem).Content.ToString();
                query = query.Where(sm => sm.MovementType == tipo);
            }

            // Filtro por fecha
            if (DpFechaDesde != null && DpFechaDesde.SelectedDate.HasValue)
            {
                query = query.Where(sm => sm.Date >= DpFechaDesde.SelectedDate.Value);
            }

            var movimientos = query
                .OrderByDescending(sm => sm.Date)
                .Select(sm => new StockMovementViewModel
                {
                    Id = sm.Id,
                    Date = sm.Date,
                    ProductName = sm.Product.Name,
                    MovementType = sm.MovementType,
                    Quantity = sm.Quantity,
                    Cost = sm.Cost,
                    SupplierName = sm.Supplier != null ? sm.Supplier.Name : null,
                    Reason = sm.Reason,
                    User = sm.User
                })
                .ToList();

            if (DgMovimientos != null)
            {
                DgMovimientos.ItemsSource = movimientos;
            }

            if (TxtTotalMovimientos != null)
            {
                TxtTotalMovimientos.Text = movimientos.Count.ToString();
            }
        }

        private void TxtBuscarProducto_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            CargarMovimientos();
        }

        private void CmbTipoMovimiento_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            CargarMovimientos();
        }

        private void DpFecha_SelectedDateChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            CargarMovimientos();
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}