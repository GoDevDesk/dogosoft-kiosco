using KioscoApp.Core.Data;
using KioscoApp.Core.Models;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Collections.Generic;

namespace KioscoApp.Client
{
    public partial class PosWindow : Window
    {
        private List<ItemVenta> _items = new();

        public PosWindow()
        {
            InitializeComponent();
            RefrescarGrid();

            // Atajos de teclado globales
            this.PreviewKeyDown += PosWindow_PreviewKeyDown;
        }

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

            TxtTotal.Text = _items.Sum(i => i.Subtotal).ToString("C");

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
                MessageBox.Show("Pago insuficiente");
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
                Date = System.DateTime.Now,
                Total = total,
                SaleType = "Interna",
                User = System.Environment.UserName,
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

        private void PosWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+Enter para cobrar
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (_items.Count > 0)
                    BtnCobrar_Click(null, null);
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
        }

        private void GridVenta_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Resaltar fila seleccionada
        }
    }

    public class ItemVenta
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
        public decimal Subtotal { get; set; }
        public int StockDisponible { get; set; }
    }
}
