using KioscoApp.Core.Data;
using KioscoApp.Core.Models;
using KioscoApp.Core.Services;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Input;

namespace KioscoApp.Client
{
    public partial class MainWindow : Window
    {
        private readonly LicenseService _licenseSvc = new LicenseService();
        private List<ItemVenta> _items = new();
        private int _listaPrecionId = 1; // ID de la lista de precios a usar (por defecto: Lista General)
        private decimal _descuentoRecargo = 0m; // Positivo = recargo, Negativo = descuento


        public MainWindow()
        {
            InitializeComponent();
            CargarListasDePrecios(); // NUEVO: Cargar listas antes de LoadData
            LoadData();
            InitializePosIntegration();
        }

        private void BtnVerListasPrecios_Click(object sender, RoutedEventArgs e)
        {
            var window = new PriceListsWindow();
            window.ShowDialog();
        }

        private void BtnNuevaListaPrecios_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new PriceListDialog();
            if (dialog.ShowDialog() == true)
            {
                // Recargar listas en el combo
                CargarListasDePrecios();
            }
        }

        private void CargarListasDePrecios()
        {
            using var ctx = new AppDbContext();
            var listas = ctx.PriceLists
                .Where(pl => pl.IsActive)
                .OrderBy(pl => pl.Name)
                .ToList();

            CmbListaPrecios.ItemsSource = listas;

            // Seleccionar la primera lista por defecto
            if (listas.Count > 0)
            {
                CmbListaPrecios.SelectedIndex = 0;
                _listaPrecionId = listas[0].Id;
            }
        }

        private void CmbListaPrecios_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (CmbListaPrecios.SelectedValue == null)
                return;

            var nuevaListaId = (int)CmbListaPrecios.SelectedValue;

            // Si hay productos en la grilla, preguntar antes de cambiar
            if (_items.Count > 0 && nuevaListaId != _listaPrecionId)
            {
                var result = MessageBox.Show(
                    "Al cambiar la lista de precios se actualizarán los precios de los productos en la venta actual.\n\n¿Desea continuar?",
                    "Confirmar cambio de lista",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    // Revertir la selección
                    CmbListaPrecios.SelectedValue = _listaPrecionId;
                    return;
                }

                // Actualizar precios de productos existentes
                ActualizarPreciosEnVenta(nuevaListaId);
            }

            _listaPrecionId = nuevaListaId;
        }

        private void ActualizarPreciosEnVenta(int nuevaListaId)
        {
            using var ctx = new AppDbContext();

            foreach (var item in _items)
            {
                var precioLista = ctx.ProductPriceLists
                    .FirstOrDefault(ppl => ppl.ProductId == item.ProductId && ppl.PriceListId == nuevaListaId);

                if (precioLista != null)
                {
                    item.Precio = precioLista.SalePrice;
                }
                else
                {
                    // Si el producto no tiene precio en la nueva lista, avisar
                    MessageBox.Show(
                        $"El producto '{item.Name}' no tiene precio definido en la lista seleccionada.\n\n" +
                        $"Se mantendrá el precio actual: {item.Precio:C}",
                        "Sin precio en lista",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }

            RefrescarGrid();
        }

        private void LoadData()
        {
            var lic = _licenseSvc.GetLicense();
            TxtLicense.Text = $"Licencia: {lic.Type}";
            TxtExpires.Text = lic.Expiry.HasValue ? $"Expira: {lic.Expiry.Value.ToShortDateString()}" : string.Empty;

            // Inicializar fecha actual
            DpFecha.SelectedDate = DateTime.Now;

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
            _descuentoRecargo = 0m; // Limpiar descuentos/recargos

            // Resetear color del texto
            TxtDescuentoRecargo.Foreground = System.Windows.Media.Brushes.White;

            RefrescarGrid();

            // Limpiar datos del cliente (mantener consumidor final)
            TxtCliente.Text = "Consumidor Final";
            TxtTelefono.Clear();
            TxtDomicilio.Clear();
            TxtLocalidad.Clear();
            CmbCondicionIva.SelectedIndex = 0;

            // Limpiar observaciones
            TxtObservaciones.Clear();

            // Reiniciar fecha
            DpFecha.SelectedDate = DateTime.Now;

            TxtCodigo.Focus();
        }

        private void BtnRecargo_Click(object sender, RoutedEventArgs e)
        {
            if (_items.Count == 0)
            {
                MessageBox.Show("Debe agregar productos antes de aplicar recargos.",
                    "Sin productos",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var subtotal = _items.Sum(i => i.Subtotal);
            var dialog = new AdjustmentDialog(subtotal, esDescuento: false);
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                _descuentoRecargo += dialog.MontoAjuste; // Sumar recargo
                RefrescarGrid();
            }
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

        #region Botones de Acciones Rápidas

        private void BtnDescuento_Click(object sender, RoutedEventArgs e)
        {
            if (_items.Count == 0)
            {
                MessageBox.Show("Debe agregar productos antes de aplicar descuentos.",
                    "Sin productos",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var subtotal = _items.Sum(i => i.Subtotal);
            var dialog = new AdjustmentDialog(subtotal, esDescuento: true);
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                _descuentoRecargo -= dialog.MontoAjuste; // Restar descuento
                RefrescarGrid();
            }
        }

        private void BtnCupon_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Funcionalidad en desarrollo", "Usar Cupón", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnPagoTarjeta_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Funcionalidad en desarrollo", "Pago con Tarjeta", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnTransferencia_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Funcionalidad en desarrollo", "Transferencia Bancaria", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            if (_items.Count > 0)
            {
                var result = MessageBox.Show("¿Está seguro que desea cancelar la venta actual?",
                                           "Cancelar Venta",
                                           MessageBoxButton.YesNo,
                                           MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _items.Clear();
                    RefrescarGrid();
                    TxtCodigo.Focus();
                    MessageBox.Show("Venta cancelada", "Información", MessageBoxButton.OK, MessageBoxImage.Information);
                }
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

            // Buscar producto e incluir sus precios
            var prod = ctx.Products
                .Include(p => p.Category)
                .FirstOrDefault(p => p.Code == TxtCodigo.Text || p.Name.Contains(TxtCodigo.Text));

            if (prod == null)
            {
                MostrarMensaje("Producto no encontrado.");
                return;
            }

            // MODIFICADO: Obtener el precio de la lista seleccionada
            var precioLista = ctx.ProductPriceLists
                .FirstOrDefault(ppl => ppl.ProductId == prod.Id && ppl.PriceListId == _listaPrecionId);

            if (precioLista == null)
            {
                MostrarMensaje($"El producto '{prod.Name}' no tiene precio en la lista seleccionada.");
                return;
            }

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
                    Precio = precioLista.SalePrice, // Usar precio de la lista seleccionada
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

            var subtotal = _items.Sum(i => i.Subtotal);
            var total = subtotal + _descuentoRecargo;

            TxtSubtotal.Text = subtotal.ToString("C");

            // Mostrar descuento/recargo acumulado
            if (_descuentoRecargo == 0)
            {
                TxtDescuentoRecargo.Text = "$0.00";
                TxtDescuentoRecargo.Foreground = System.Windows.Media.Brushes.White;
            }
            else if (_descuentoRecargo < 0)
            {
                TxtDescuentoRecargo.Text = $"-{Math.Abs(_descuentoRecargo):C}";
                TxtDescuentoRecargo.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(230, 126, 34)); // Naranja para descuento
            }
            else
            {
                TxtDescuentoRecargo.Text = $"+{_descuentoRecargo:C}";
                TxtDescuentoRecargo.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(39, 174, 96)); // Verde para recargo
            }

            TxtTotal.Text = total.ToString("C");

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
            if (_items.Count == 0)
                return;

            var total = _items.Sum(i => i.Subtotal);

            using var ctx = new AppDbContext();

            // Validar stock antes de abrir ventana de pago
            foreach (var item in _items)
            {
                var prod = ctx.Products.FirstOrDefault(p => p.Id == item.ProductId);
                if (prod == null || prod.Stock < item.Cantidad)
                {
                    MessageBox.Show($"No hay stock suficiente de {item.Name}. Disponible: {prod?.Stock ?? 0}");
                    return;
                }
            }

            // Abrir ventana modal de pago
            var paymentWindow = new PaymentWindow(total);
            paymentWindow.Owner = this;

            var result = paymentWindow.ShowDialog();

            if (result == true && paymentWindow.PagoConfirmado)
            {
                // Crear la venta
                var sale = new Sale
                {
                    Date = DpFecha.SelectedDate ?? DateTime.Now,
                    Total = total,
                    SaleType = CmbTipoTicket.Text,
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

                // NUEVO: Crear movimientos de stock por cada item vendido
                var fechaVenta = DpFecha.SelectedDate ?? DateTime.Now;
                var usuario = Environment.UserName;
                var numeroVenta = $"{CmbTipoTicket.Text} - Cliente: {TxtCliente.Text}";

                foreach (var item in _items)
                {
                    // Actualizar stock del producto
                    var prod = ctx.Products.FirstOrDefault(p => p.Id == item.ProductId);
                    if (prod != null)
                    {
                        prod.Stock -= item.Cantidad;

                        // Registrar movimiento de stock negativo (venta)
                        var movimiento = new StockMovement
                        {
                            ProductId = item.ProductId,
                            SupplierId = null, // Las ventas no tienen proveedor
                            MovementType = "Venta",
                            Quantity = -item.Cantidad, // Negativo porque sale del inventario
                            Cost = null, // Las ventas no tienen costo, tienen precio de venta
                            Reason = $"Venta - {numeroVenta}",
                            Date = fechaVenta,
                            User = usuario
                        };

                        ctx.StockMovements.Add(movimiento);
                    }
                }

                ctx.SaveChanges();

                // Calcular vuelto con la nueva lógica
                var vuelto = paymentWindow.TotalPagado - total;

                // Preparar info de métodos de pago
                string pagosDetalle =
                    $"Efectivo: {paymentWindow.Efectivo:C}\n" +
                    $"Tarjeta: {paymentWindow.TarjetaCredito:C}\n" +
                    $"Cuenta Corriente: {paymentWindow.CuentaCorriente:C}\n" +
                    $"Cheque: {paymentWindow.Cheque:C}\n" +
                    $"Tickets: {paymentWindow.Tickets:C}";

                // Mostrar mensaje de éxito
                var clienteInfo = TxtCliente.Text != "Consumidor Final" ? $"\nCliente: {TxtCliente.Text}" : "";
                MessageBox.Show($"✓ Venta registrada correctamente{clienteInfo}\n" +
                               $"Total: {total:C}\n" +
                               $"Pagado: {paymentWindow.TotalPagado:C}\n" +
                               $"{pagosDetalle}\n" +
                               $"Vuelto: {vuelto:C}\n\n" +
                               $"Se registraron {_items.Count} movimientos de stock.",
                               "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                // Limpiar para nueva venta
                BtnNuevaVenta_Click(sender, e);
            }
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
                    if (nuevaCantidad > selectedItem.StockDisponible)
                    {
                        MessageBox.Show($"Cantidad excede el stock disponible ({selectedItem.StockDisponible})");
                        return;
                    }
                    selectedItem.Cantidad = nuevaCantidad;
                    RefrescarGrid();
                }
            }
        }

        private void BtnCambiarPrecio_Click(object sender, RoutedEventArgs e)
        {
            if (GridVenta.SelectedItem is ItemVenta selectedItem)
            {
                var input = Microsoft.VisualBasic.Interaction.InputBox(
                    $"Ingrese nuevo precio para {selectedItem.Name}:",
                    "Cambiar Precio",
                    selectedItem.Precio.ToString());

                if (decimal.TryParse(input, out decimal nuevoPrecio) && nuevoPrecio > 0)
                {
                    selectedItem.Precio = nuevoPrecio;
                    RefrescarGrid();
                }
            }
        }

        private void GridVenta_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Resaltar fila seleccionada - ya manejado por el DataGrid
        }

        #endregion

        #region Datos del Cliente

        private void BtnCambiarCliente_Click(object sender, RoutedEventArgs e)
        {
            // Aquí podrías abrir un diálogo para seleccionar cliente
            // Por ahora, mensaje de desarrollo
            MessageBox.Show("Aquí se abriría un diálogo para seleccionar cliente de la base de datos",
                           "Cambiar Cliente", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Atajos de Teclado
        private void BtnVerPrecio_Click(object sender, RoutedEventArgs e)
        {
            var input = Microsoft.VisualBasic.Interaction.InputBox(
                "Ingrese código o nombre del producto:",
                "Ver Precio",
                "");

            if (string.IsNullOrWhiteSpace(input))
                return;

            using var ctx = new AppDbContext();
            var prod = ctx.Products.FirstOrDefault(p => p.Code == input || p.Name.Contains(input));

            if (prod != null)
            {
                // Obtener precio de la lista actual
                var precioLista = ctx.ProductPriceLists
                    .Include(ppl => ppl.PriceList)
                    .FirstOrDefault(ppl => ppl.ProductId == prod.Id && ppl.PriceListId == _listaPrecionId);

                if (precioLista != null)
                {
                    MessageBox.Show($"Producto: {prod.Name}\n" +
                                  $"Lista: {precioLista.PriceList.Name}\n" +
                                  $"Precio: {precioLista.SalePrice:C}\n" +
                                  $"Stock: {prod.Stock}",
                                   "Información del Producto",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Producto: {prod.Name}\n" +
                                  $"Sin precio en la lista '{CmbListaPrecios.Text}'\n" +
                                  $"Stock: {prod.Stock}",
                                   "Información del Producto",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Warning);
                }
            }
            else
            {
                MessageBox.Show("Producto no encontrado", "Información", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // F1 para cobrar
            if (e.Key == Key.F1)
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

            // F2 para productos
            if (e.Key == Key.F2)
            {
                BtnNuevaVenta_Click(sender, e);
                e.Handled = true;
            }

            // F3 para ver precio
            if (e.Key == Key.F3)
            {
                BtnVerPrecio_Click(sender, e);
                e.Handled = true;
            }

            // F4 para búsqueda avanzada
            if (e.Key == Key.F4)
            {
                BtnBuscarAvanzado_Click(sender, e);
                e.Handled = true;
            }

            // F5 para cambiar cliente
            if (e.Key == Key.F5)
            {
                BtnCambiarCliente_Click(sender, e);
                e.Handled = true;
            }

            // Enter en campo código
            if (e.Key == Key.Enter && TxtCodigo.IsFocused)
            {
                AgregarProducto();
                e.Handled = true;
            }
        }

        private void BtnNuevoArticulo_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new ProductDialog();
            if (dlg.ShowDialog() == true)
            {
                // no hace falta nada más, ProductDialog guarda directo en DB
            }
        }

        private void BtnEditarArticulo_Click(object sender, RoutedEventArgs e)
        {
            var win = new ProductsWindow();
            win.ShowDialog();
            // ahí el usuario selecciona un producto y hace clic en "Modificar"
        }

        private void BtnVerArticulos_Click(object sender, RoutedEventArgs e)
        {
            var win = new ProductsWindow();
            win.ShowDialog();
        }


        // Agregar estos métodos en la región "Eventos del Menú" de MainWindow.xaml.cs

        #region Eventos del Menú - Proveedores

        private void BtnNuevoProveedor_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SupplierDialog();
            if (dialog.ShowDialog() == true)
            {
                MessageBox.Show("Proveedor creado correctamente.", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnVerProveedores_Click(object sender, RoutedEventArgs e)
        {
            var window = new SuppliersWindow();
            window.ShowDialog();
        }

        private void BtnCargarCompra_Click(object sender, RoutedEventArgs e)
        {
            var window = new PurchaseOrderWindow();
            if (window.ShowDialog() == true)
            {
                // Compra guardada exitosamente
            }
        }

        private void BtnMovimientosStock_Click(object sender, RoutedEventArgs e)
        {
            var window = new StockMovementsWindow();
            window.ShowDialog();
        }

        private void BtnAjusteInventario_Click(object sender, RoutedEventArgs e)
        {
            var window = new InventoryAdjustmentWindow();
            window.ShowDialog();
        }

        private void MenuActualizacionMasivaPrecios_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new PriceUpdateDialog();
            dialog.ShowDialog();
        }

        private void BtnBuscarAvanzado_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ProductSearchDialog(_listaPrecionId);
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                // Agregar todos los productos seleccionados a la venta
                foreach (var producto in dialog.ProductosSeleccionados)
                {
                    var itemExistente = _items.FirstOrDefault(i => i.ProductId == producto.ProductId);

                    if (itemExistente != null)
                    {
                        // Si ya existe, sumar cantidades
                        itemExistente.Cantidad += producto.Cantidad;
                    }
                    else
                    {
                        // Agregar nuevo
                        _items.Add(new ItemVenta
                        {
                            ProductId = producto.ProductId,
                            Name = producto.Name,
                            Cantidad = producto.Cantidad,
                            Precio = producto.Price,
                            StockDisponible = producto.Stock
                        });
                    }
                }

                RefrescarGrid();

                MessageBox.Show(
                    $"Se agregaron {dialog.ProductosSeleccionados.Count} productos a la venta.",
                    "Productos agregados",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        #endregion
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