using FastFoodApp.Core.Data;
using FastFoodApp.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace FastFoodApp.Client
{
    public partial class InventoryAdjustmentWindow : Window
    {
        private Product? _productoSeleccionado;

        public InventoryAdjustmentWindow()
        {
            InitializeComponent();
            CargarHistorial();
        }

        private void TxtBuscarProducto_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BuscarProducto();
                e.Handled = true;
            }
        }

        private void BtnBuscar_Click(object sender, RoutedEventArgs e)
        {
            BuscarProducto();
        }

        private void BuscarProducto()
        {
            if (string.IsNullOrWhiteSpace(TxtBuscarProducto.Text))
                return;

            using var ctx = new AppDbContext();
            var producto = ctx.Products.FirstOrDefault(p =>
                p.Code == TxtBuscarProducto.Text ||
                p.Name.Contains(TxtBuscarProducto.Text));

            if (producto == null)
            {
                MessageBox.Show("Producto no encontrado.", "Información",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _productoSeleccionado = producto;
            MostrarProducto();
        }

        private void MostrarProducto()
        {
            if (_productoSeleccionado == null)
                return;

            TxtProductoNombre.Text = $"{_productoSeleccionado.Code} - {_productoSeleccionado.Name}";
            TxtStockActual.Text = _productoSeleccionado.Stock.ToString();
            TxtCantidad.Text = "0";
            TxtMotivo.Clear();

            PanelProducto.Visibility = Visibility.Visible;
            BtnGuardar.IsEnabled = true;
            TxtCantidad.Focus();
            TxtCantidad.SelectAll();
        }

        private void CmbTipoAjuste_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Opcional: cambiar color o comportamiento según el tipo
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (_productoSeleccionado == null)
                return;

            // Validaciones
            if (!int.TryParse(TxtCantidad.Text, out int cantidad) || cantidad <= 0)
            {
                MessageBox.Show("La cantidad debe ser un número mayor a cero.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtCantidad.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtMotivo.Text))
            {
                MessageBox.Show("Debe especificar el motivo del ajuste.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtMotivo.Focus();
                return;
            }

            var tipoAjuste = ((System.Windows.Controls.ComboBoxItem)CmbTipoAjuste.SelectedItem).Content.ToString();
            var cantidadFinal = tipoAjuste == "Ajuste +" ? cantidad : -cantidad;

            // Validar que no quede stock negativo
            if (tipoAjuste == "Ajuste -" && _productoSeleccionado.Stock < cantidad)
            {
                var result = MessageBox.Show(
                    $"El stock actual es {_productoSeleccionado.Stock} y está intentando restar {cantidad}.\n" +
                    $"Esto dejará el stock en negativo. ¿Desea continuar?",
                    "Advertencia",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                    return;
            }

            try
            {
                using var ctx = new AppDbContext();

                // Crear movimiento
                var movimiento = new StockMovement
                {
                    ProductId = _productoSeleccionado.Id,
                    SupplierId = null,
                    MovementType = tipoAjuste!,
                    Quantity = cantidadFinal,
                    Cost = null,
                    Reason = TxtMotivo.Text.Trim(),
                    Date = DateTime.Now,
                    User = Environment.UserName
                };

                ctx.StockMovements.Add(movimiento);

                // Actualizar stock del producto
                var producto = ctx.Products.Find(_productoSeleccionado.Id);
                if (producto != null)
                {
                    producto.Stock += cantidadFinal;
                }

                ctx.SaveChanges();

                MessageBox.Show(
                    $"Ajuste registrado correctamente.\n" +
                    $"Stock anterior: {_productoSeleccionado.Stock}\n" +
                    $"Stock nuevo: {producto?.Stock}",
                    "Éxito",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Limpiar y recargar
                _productoSeleccionado = null;
                PanelProducto.Visibility = Visibility.Collapsed;
                BtnGuardar.IsEnabled = false;
                TxtBuscarProducto.Clear();
                TxtBuscarProducto.Focus();
                CargarHistorial();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar el ajuste:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CargarHistorial()
        {
            using var ctx = new AppDbContext();

            var historial = ctx.StockMovements
                .Include(sm => sm.Product)
                .Where(sm => sm.MovementType == "Ajuste +" || sm.MovementType == "Ajuste -")
                .OrderByDescending(sm => sm.Date)
                .Take(50)
                .Select(sm => new
                {
                    sm.Date,
                    ProductName = sm.Product.Name,
                    sm.MovementType,
                    sm.Quantity,
                    sm.Reason
                })
                .ToList();

            DgHistorial.ItemsSource = historial;
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}