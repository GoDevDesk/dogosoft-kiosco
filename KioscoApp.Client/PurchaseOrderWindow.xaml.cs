using KioscoApp.Core.Data;
using KioscoApp.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KioscoApp.Client
{
    public class PurchaseItem : INotifyPropertyChanged
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";

        private int _quantity;
        public int Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged(nameof(Quantity));
                    OnPropertyChanged(nameof(Subtotal));
                }
            }
        }

        private decimal _cost;
        public decimal Cost
        {
            get => _cost;
            set
            {
                if (_cost != value)
                {
                    _cost = value;
                    OnPropertyChanged(nameof(Cost));
                    OnPropertyChanged(nameof(Subtotal));
                }
            }
        }

        public decimal Subtotal => Quantity * Cost;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SupplierProduct
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public decimal Cost { get; set; }
        public string DisplayText => $"{ProductCode} - {ProductName} (${Cost:N2})";
        public string SearchText => $"{ProductCode} {ProductName}".ToLower();
    }

    public partial class PurchaseOrderWindow : Window
    {
        private ObservableCollection<PurchaseItem> _items;
        private List<SupplierProduct> _todosLosProductos;
        private bool _isUpdatingText = false;

        public PurchaseOrderWindow()
        {
            InitializeComponent();
            _items = new ObservableCollection<PurchaseItem>();
            _todosLosProductos = new List<SupplierProduct>();
            DgProductos.ItemsSource = _items;
            DpFecha.SelectedDate = DateTime.Now;
            CargarProveedores();

            // Suscribirse a cambios en los items para actualizar totales
            _items.CollectionChanged += (s, e) => ActualizarTotal();

            // IMPORTANTE: Asegurar que la ventana pueda recibir el foco
            this.Loaded += (s, e) =>
            {
                this.Focus();
            };
        }


        private void CargarProveedores()
        {
            using var ctx = new AppDbContext();
            var proveedores = ctx.Suppliers.Where(s => s.IsActive).OrderBy(s => s.Name).ToList();
            CmbProveedor.ItemsSource = proveedores;
        }

        private void CmbProveedor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbProveedor.SelectedItem == null)
            {
                CmbProducto.ItemsSource = null;
                CmbProducto.IsEnabled = false;
                BtnAgregar.IsEnabled = false;
                _todosLosProductos.Clear();
                return;
            }

            // NUEVA VALIDACIÓN: Si ya hay productos agregados, preguntar antes de cambiar
            if (_items.Count > 0 && e.RemovedItems.Count > 0)
            {
                var result = MessageBox.Show(
                    "Al cambiar de proveedor se perderán los productos ya agregados.\n\n¿Desea continuar?",
                    "Confirmar cambio de proveedor",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                {
                    // Revertir la selección al proveedor anterior
                    _isUpdatingText = true; // Usar esta bandera para evitar loops
                    CmbProveedor.SelectedItem = e.RemovedItems[0];
                    _isUpdatingText = false;
                    return;
                }

                // Si acepta, limpiar la lista de productos
                _items.Clear();
                ActualizarTotal();
            }

            var proveedor = (Supplier)CmbProveedor.SelectedItem;
            CargarProductosDelProveedor(proveedor.Id);
        }

        private void CargarProductosDelProveedor(int supplierId)
        {
            using var ctx = new AppDbContext();

            // Obtener productos asociados a este proveedor
            var productos = ctx.ProductSuppliers
                .Include(ps => ps.Product)
                .Where(ps => ps.SupplierId == supplierId && ps.Product.IsActive)
                .Select(ps => new SupplierProduct
                {
                    ProductId = ps.ProductId,
                    ProductCode = ps.Product.Code,
                    ProductName = ps.Product.Name,
                    Cost = ps.Cost
                })
                .OrderBy(sp => sp.ProductName)
                .ToList();

            if (productos.Count == 0)
            {
                MessageBox.Show(
                    "Este proveedor no tiene productos asociados.\n\n" +
                    "Puede asociar productos al proveedor desde la ventana de edición del producto, " +
                    "en la pestaña 'Proveedores'.",
                    "Sin productos",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                CmbProducto.ItemsSource = null;
                CmbProducto.IsEnabled = false;
                BtnAgregar.IsEnabled = false;
                _todosLosProductos.Clear();
            }
            else
            {
                _todosLosProductos = productos;
                CmbProducto.ItemsSource = productos;
                CmbProducto.IsEnabled = true;
                BtnAgregar.IsEnabled = true;
            }
        }

        private void CmbProducto_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingText || sender is not ComboBox comboBox)
                return;

            var filtro = comboBox.Text.ToLower().Trim();

            if (string.IsNullOrWhiteSpace(filtro))
            {
                comboBox.ItemsSource = _todosLosProductos;
            }
            else
            {
                var productosFiltrados = _todosLosProductos
                    .Where(p => p.SearchText.Contains(filtro))
                    .ToList();

                comboBox.ItemsSource = productosFiltrados;
            }

            // Abrir dropdown si hay texto y resultados
            if (!string.IsNullOrWhiteSpace(filtro) && comboBox.Items.Count > 0)
            {
                comboBox.IsDropDownOpen = true;
            }
        }

        private void CmbProducto_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem is SupplierProduct producto)
            {
                // Forzar que el texto refleje la selección
                _isUpdatingText = true;
                comboBox.Text = producto.DisplayText;
                _isUpdatingText = false;

                // Cerrar el dropdown
                comboBox.IsDropDownOpen = false;
            }
        }

        private void BtnAgregar_Click(object sender, RoutedEventArgs e)
        {
            SupplierProduct? productoSeleccionado = null;

            // Intentar obtener el producto seleccionado
            if (CmbProducto.SelectedItem is SupplierProduct producto)
            {
                productoSeleccionado = producto;
            }
            else if (!string.IsNullOrWhiteSpace(CmbProducto.Text))
            {
                // Buscar por el texto si no hay selección directa
                var texto = CmbProducto.Text.Trim();
                productoSeleccionado = _todosLosProductos
                    .FirstOrDefault(p => p.DisplayText.Equals(texto, StringComparison.OrdinalIgnoreCase));
            }

            if (productoSeleccionado == null)
            {
                MessageBox.Show("Debe seleccionar un producto de la lista.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CmbProducto.Focus();
                return;
            }

            // Verificar si ya está en la lista
            var itemExistente = _items.FirstOrDefault(i => i.ProductId == productoSeleccionado.ProductId);
            if (itemExistente != null)
            {
                itemExistente.Quantity++;
                DgProductos.Items.Refresh();
            }
            else
            {
                var nuevoItem = new PurchaseItem
                {
                    ProductId = productoSeleccionado.ProductId,
                    ProductCode = productoSeleccionado.ProductCode,
                    ProductName = productoSeleccionado.ProductName,
                    Quantity = 1,
                    Cost = productoSeleccionado.Cost
                };

                // Suscribirse a cambios de propiedades para actualizar el total
                nuevoItem.PropertyChanged += (s, ev) =>
                {
                    if (ev.PropertyName == nameof(PurchaseItem.Subtotal))
                    {
                        ActualizarTotal();
                    }
                };

                _items.Add(nuevoItem);
            }

            // Limpiar el ComboBox
            CmbProducto.SelectedIndex = -1;
            CmbProducto.Text = string.Empty;
            CmbProducto.ItemsSource = _todosLosProductos;

            ActualizarTotal();

            // IMPORTANTE: Mover el foco al DataGrid para salir del ComboBox
            // Esto permite usar F10 sin problema
            DgProductos.Focus();

            // Si quieres seleccionar el último item agregado:
            if (_items.Count > 0)
            {
                DgProductos.SelectedIndex = _items.Count - 1;
                DgProductos.ScrollIntoView(_items[_items.Count - 1]);
            }
        }

        private void BtnQuitar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is PurchaseItem item)
            {
                _items.Remove(item);
                ActualizarTotal();
            }
        }

        private void ActualizarTotal()
        {
            var total = _items.Sum(i => i.Subtotal);
            TxtTotal.Text = total.ToString("C");
        }

        private void BtnAyuda_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "CÓMO CARGAR UNA COMPRA:\n\n" +
                "1️⃣ Seleccione el PROVEEDOR del cual está comprando.\n\n" +
                "2️⃣ Seleccione la FECHA de la compra.\n\n" +
                "3️⃣ Busque y agregue PRODUCTOS:\n" +
                "   • Puede escribir en el campo para filtrar productos\n" +
                "   • Se mostrarán solo productos asociados al proveedor\n" +
                "   • El costo se carga automáticamente\n" +
                "   • Presione F2 o Enter para agregar\n\n" +
                "4️⃣ Ajuste CANTIDAD y COSTO si es necesario:\n" +
                "   • Haga doble clic en las celdas para editar\n" +
                "   • El subtotal se calcula automáticamente\n\n" +
                "5️⃣ Presione F9 o Ctrl+S para GUARDAR.\n\n" +
                "⌨️ ATAJOS DE TECLADO:\n" +
                "   F1 - Esta ayuda\n" +
                "   F2 - Agregar producto\n" +
                "   F9 o Ctrl+S - Guardar compra\n" +
                "   Esc - Cancelar\n\n" +
                "💡 El stock se actualizará automáticamente al guardar.",
                "Ayuda - Cargar Compra",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // Validaciones
            if (CmbProveedor.SelectedItem == null)
            {
                MessageBox.Show("Debe seleccionar un proveedor.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_items.Count == 0)
            {
                MessageBox.Show("Debe agregar al menos un producto.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validar que todos los productos tengan cantidad y costo
            if (_items.Any(i => i.Quantity <= 0 || i.Cost <= 0))
            {
                MessageBox.Show("Todos los productos deben tener cantidad y costo mayor a cero.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // NUEVA CONFIRMACIÓN antes de guardar
            var proveedor = (Supplier)CmbProveedor.SelectedItem;
            var confirmacion = MessageBox.Show(
                $"¿Confirma que desea guardar esta compra?\n\n" +
                $"Proveedor: {proveedor.Name}\n" +
                $"Productos: {_items.Count}\n" +
                $"Total: {TxtTotal.Text}\n\n" +
                $"Se actualizará el stock automáticamente.",
                "Confirmar Compra",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmacion != MessageBoxResult.Yes)
                return;

            try
            {
                using var ctx = new AppDbContext();
                var fecha = DpFecha.SelectedDate ?? DateTime.Now;
                var usuario = Environment.UserName;

                // Crear movimientos de stock por cada producto
                foreach (var item in _items)
                {
                    var movimiento = new StockMovement
                    {
                        ProductId = item.ProductId,
                        SupplierId = proveedor.Id,
                        MovementType = "Compra",
                        Quantity = item.Quantity,
                        Cost = item.Cost,
                        Reason = $"Compra a {proveedor.Name}",
                        Date = fecha,
                        User = usuario
                    };

                    ctx.StockMovements.Add(movimiento);

                    // Actualizar stock del producto
                    var producto = ctx.Products.Find(item.ProductId);
                    if (producto != null)
                    {
                        producto.Stock += item.Quantity;
                    }

                    // Actualizar el costo en ProductSupplier si cambió
                    var productSupplier = ctx.ProductSuppliers
                        .FirstOrDefault(ps => ps.ProductId == item.ProductId && ps.SupplierId == proveedor.Id);

                    if (productSupplier != null && productSupplier.Cost != item.Cost)
                    {
                        productSupplier.Cost = item.Cost;
                        productSupplier.LastUpdate = DateTime.Now;
                    }
                }

                ctx.SaveChanges();

                MessageBox.Show(
                    $"✓ Compra registrada correctamente.\n\n" +
                    $"Proveedor: {proveedor.Name}\n" +
                    $"Productos: {_items.Count}\n" +
                    $"Total: {TxtTotal.Text}\n\n" +
                    $"El stock ha sido actualizado.",
                    "Éxito",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar la compra:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            if (_items.Count > 0)
            {
                var result = MessageBox.Show("¿Está seguro que desea cancelar? Se perderán los datos.",
                    "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    // NO hacer nada, simplemente return
                    return;
                }
            }

            // Solo cerrar si dijo que SÍ o si no hay items
            DialogResult = false;
            Close();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F1:
                    BtnAyuda_Click(sender, e);
                    e.Handled = true;
                    break;

                case Key.F2:
                    if (BtnAgregar.IsEnabled)
                    {
                        BtnAgregar_Click(sender, e);
                        e.Handled = true;
                    }
                    break;

                case Key.F9:
                    BtnGuardar_Click(sender, e);
                    e.Handled = true;
                    break;

                case Key.S:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                    {
                        BtnGuardar_Click(sender, e);
                        e.Handled = true;
                    }
                    break;

                case Key.Escape:
                    BtnCancelar_Click(sender, e);
                    e.Handled = true;
                    break;
            }
        }
    }
}