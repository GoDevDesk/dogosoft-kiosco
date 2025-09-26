using System;
using System.Linq;
using System.Windows;
using KioscoApp.Core.Data;
using KioscoApp.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace KioscoApp.Client
{
    public partial class SalesHistoryWindow : Window
    {
        private List<Sale> _ventas = new();

        public SalesHistoryWindow()
        {
            InitializeComponent();

            DpDesde.SelectedDate = DateTime.Today;
            DpHasta.SelectedDate = DateTime.Today;

            CargarFiltros();
            CargarVentas();
        }

        private void CargarFiltros()
        {
            using var ctx = new AppDbContext();

            var usuarios = ctx.Sales.Select(s => s.User).Distinct().ToList();
            CbUsuario.ItemsSource = usuarios;
            CbUsuario.SelectedIndex = -1;

            var tipos = ctx.Sales.Select(s => s.SaleType).Distinct().ToList();
            CbTipo.ItemsSource = tipos;
            CbTipo.SelectedIndex = -1;
        }

        private void CargarVentas()
        {
            using var ctx = new AppDbContext();
            _ventas = ctx.Sales
                         .OrderByDescending(s => s.Date)
                         .ToList();

            GridSales.ItemsSource = _ventas;
        }

        private void BtnFiltrar_Click(object sender, RoutedEventArgs e)
        {
            var ventasFiltradas = _ventas.AsEnumerable();

            // Filtrar por fecha
            if (DpDesde.SelectedDate.HasValue && DpHasta.SelectedDate.HasValue)
            {
                var desde = DpDesde.SelectedDate.Value.Date;
                var hasta = DpHasta.SelectedDate.Value.Date.AddDays(1).AddSeconds(-1);
                ventasFiltradas = ventasFiltradas.Where(s => s.Date >= desde && s.Date <= hasta);
            }

            // Filtrar por usuario
            if (CbUsuario.SelectedItem != null)
                ventasFiltradas = ventasFiltradas.Where(s => s.User == CbUsuario.SelectedItem.ToString());

            // Filtrar por tipo
            if (CbTipo.SelectedItem != null)
                ventasFiltradas = ventasFiltradas.Where(s => s.SaleType == CbTipo.SelectedItem.ToString());

            GridSales.ItemsSource = ventasFiltradas.OrderByDescending(s => s.Date).ToList();
        }

        private void GridSales_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (GridSales.SelectedItem is Sale sale)
            {
                using var ctx = new AppDbContext();
                var items = ctx.Sales
                               .Include(s => s.Items)
                               .ThenInclude(i => i.Product)
                               .FirstOrDefault(s => s.Id == sale.Id)?
                               .Items
                               .ToList();

                if (items != null && items.Count > 0)
                {
                    var detailsWindow = new SaleDetailsWindow(items);
                    detailsWindow.ShowDialog();
                }
                else
                {
                    MessageBox.Show("No se encontraron detalles para esta venta.");
                }
            }
        }

        private void BtnExportar_Click(object sender, RoutedEventArgs e)
        {
            if (GridSales.ItemsSource is not IEnumerable<Sale> ventasExportar) return;

            var sb = new StringBuilder();
            sb.AppendLine("ID,Fecha,Usuario,Tipo,Total");

            foreach (var v in ventasExportar)
            {
                sb.AppendLine($"{v.Id},{v.Date},{v.User},{v.SaleType},{v.Total}");
            }

            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ventas.csv");
            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);

            MessageBox.Show($"Ventas exportadas a {filePath}");
        }
    }
}
