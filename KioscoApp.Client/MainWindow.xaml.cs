using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using KioscoApp.Core.Data;
using KioscoApp.Core.Models;
using KioscoApp.Core.Services;

namespace KioscoApp.Client
{
    public partial class MainWindow : Window
    {
        private readonly LicenseService _licenseSvc = new LicenseService();
        private List<ItemVenta> _items = new();

        public MainWindow()
        {
            InitializeComponent();
            LoadData();
            InitializePosIntegration();
        }

        private void LoadData()
        {
            var lic = _licenseSvc.GetLicense();
            TxtLicense.Text = $"Licencia: {lic.Type}";
            TxtExpires.Text = lic.Expiry.HasValue ? $"Expira: {lic.Expiry.Value.ToShortDateString()}" : string.Empty;

            RefrescarGrid();
        }

        private void InitializePosIntegration()
        {
            // Atajos de teclado globales para el POS integrado
            this.PreviewKeyDown += MainWindow_PreviewKeyDown;

            // Focus inicial en el campo de código
            TxtCodigo.Focus();
        }

        #region Eventos del Menú

        private void BtnNuevaVenta_Click(object sender, RoutedEventArgs e)
        {
            // Limpiar venta actual
            _items.Clear();
            RefrescarGrid();
            TxtPago.Clear();
            TxtCodigo.Focus();
        }

        private void BtnProductos_Click(object sender, RoutedEventArgs e)
        {
            var window = new ProductsWindow();
            window.ShowDialog();
        }

        private void BtnHistorialVentas_Click(object sender, RoutedEventArgs e)
        {
            var window = new SalesHistoryWindow();
            window.ShowDialog();
        }

        private void BtnReportes_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Funcionalidad en desarrollo", "Reportes", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnClientes_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Funcionalidad en desarrollo", "Clientes", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnInventario_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Funcionalidad en desarrollo", "Inventario", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnConfiguracion_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Funcionalidad en desarrollo", "Configuración", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnIngresarClave_Click(object sender, RoutedEventArgs e)
        {
            var input = new SimpleInputDialog("Ingrese clave de licencia:").ShowDialogAndReturn();
            if (!string.IsNullOrWhiteSpace(input))
            {
                _licenseSvc.ActivateLicense(input, "Paga", null);
                MessageBox.Show("Licencia activada. Reinicie la aplicación.");
                LoadData();
            }
        }

        #endregion

        #region Funcionalidad POS Integrada

        private void BtnAgregar_Click(object sender, RoutedEventArgs e)
        {
            AgregarProducto();
        }

        private void TxtCodigo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AgregarProducto();
                e.Handled = true;
            }
        }

        private void AgregarProducto()
        {
            using var ctx = new AppDbContext();
            var prod = ctx.Products.FirstOrDefault(p => p.Code == TxtCodigo.Text || p.Name.Contains(TxtCodigo.Text));

            if (prod == null)
                return; // no mostrar mensaje innecesario

            var item = _items.FirstOrDefault(i => i.ProductId == prod.Id);

            if (item != null)
            {
                if (item.Cantidad + 1 > prod.Stock)
                {
                    MostrarMensaje($"No hay stock suficiente para {prod.Name}. Stock disponible: {prod.Stock}");
                    return;
                }
                item.Cantidad++;
            }
            else
            {
                if (prod.Stock <= 0)
                {
                    MostrarMensaje($"No hay stock disponible para {prod.Name}.");
                    return;
                }

                _items.Add(new ItemVenta
                {
                    ProductId = prod.Id,
                    Name = prod.Name,
                    Cantidad = 1,
                    Precio = prod.Price,
                    StockDisponible = prod.Stock
                });
            }

            RefrescarGrid();
            TxtCodigo.Clear();
            TxtCodigo.Focus();
        }

        private void RefrescarGrid()
        {
            foreach (var i in _items)
                i.Subtotal = i.Cantidad * i.Precio;

            GridVenta.ItemsSource = null;
            GridVenta.ItemsSource = _items;

            var total = _items.Sum(i => i.Subtotal);
            TxtTotalGrande.Text = total.ToString("C");

            // Habilitar o deshabilitar botón cobrar
            BtnCobrar.IsEnabled = _items.Count > 0;
        }

        private async void MostrarMensaje(string mensaje)
        {
            TxtMensaje.Text = mensaje;
            await Task.Delay(3000); // 3 segundos
            TxtMensaje.Text = "";
        }

        private void BtnCobrar_Click(object sender, RoutedEventArgs e)
        {
            decimal pago = 0;
            decimal.TryParse(TxtPago.Text, out pago);

            var total = _items.Sum(i => i.Subtotal);

            // Si TxtPago está vacío se toma como pago exacto
            if (pago == 0)
                pago = total;

            if (pago < total)
            {
                MessageBox.Show("Pago insuficiente", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var ctx = new AppDbContext();

            // Validar stock antes de registrar
            foreach (var item in _items)
            {
                var prod = ctx.Products.FirstOrDefault(p => p.Id == item.ProductId);
                if (prod == null || prod.Stock < item.Cantidad)
                {
                    MessageBox.Show($"No hay stock suficiente de {item.Name}. Disponible: {prod?.Stock ?? 0}");
                    return;
                }
            }

            var vuelto = pago - total;

            var sale = new Sale
            {
                Date = DateTime.Now,
                Total = total,
                SaleType = "Interna",
                User = Environment.UserName,
                Items = _items.Select(i => new SaleItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Cantidad,
                    UnitPrice = i.Precio,
                    Subtotal = i.Subtotal
                }).ToList()
            };

            ctx.Sales.Add(sale);

            // Actualizar stock
            foreach (var item in sale.Items)
            {
                var prod = ctx.Products.FirstOrDefault(p => p.Id == item.ProductId);
                if (prod != null)
                    prod.Stock -= item.Quantity;
            }

            ctx.SaveChanges();

            _items.Clear();
            RefrescarGrid();
            TxtPago.Clear();

            // Mostrar mensaje de éxito
            MessageBox.Show($"Venta registrada correctamente.\nVuelto: {vuelto:C}", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

            TxtCodigo.Focus();
        }

        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (GridVenta.SelectedItem is ItemVenta selectedItem)
            {
                if (selectedItem.Cantidad > 1)
                    selectedItem.Cantidad--;
                else
                    _items.Remove(selectedItem);

                RefrescarGrid();
            }
        }

        private void BtnModificarCantidad_Click(object sender, RoutedEventArgs e)
        {
            if (GridVenta.SelectedItem is ItemVenta selectedItem)
            {
                var input = Microsoft.VisualBasic.Interaction.InputBox(
                    $"Ingrese nueva cantidad para {selectedItem.Name}:",
                    "Modificar Cantidad",
                    selectedItem.Cantidad.ToString());

                if (int.TryParse(input, out int nuevaCantidad) && nuevaCantidad > 0)
                {
                    selectedItem.Cantidad = nuevaCantidad;
                    RefrescarGrid();
                }
            }
        }

        private void GridVenta_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Resaltar fila seleccionada - ya manejado por el DataGrid
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+Enter para cobrar
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (_items.Count > 0)
                    BtnCobrar_Click(sender, e);
                e.Handled = true;
            }

            // Delete para descontar o eliminar ítem
            if (e.Key == Key.Delete && GridVenta.SelectedItem is ItemVenta selectedItem)
            {
                if (selectedItem.Cantidad > 1)
                    selectedItem.Cantidad--;
                else
                    _items.Remove(selectedItem);

                RefrescarGrid();
                e.Handled = true;
            }

            // F1 para nueva venta
            if (e.Key == Key.F1)
            {
                BtnNuevaVenta_Click(sender, e);
                e.Handled = true;
            }

            // F2 para productos
            if (e.Key == Key.F2)
            {
                BtnProductos_Click(sender, e);
                e.Handled = true;
            }

            // F3 para historial
            if (e.Key == Key.F3)
            {
                BtnHistorialVentas_Click(sender, e);
                e.Handled = true;
            }
        }

        #endregion
    }

    // Clase ItemVenta para el POS integrado
    public class ItemVenta
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
        public decimal Subtotal { get; set; }
        public int StockDisponible { get; set; }
    }
}