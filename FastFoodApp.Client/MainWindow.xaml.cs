using FastFoodApp.Core.Data;
using FastFoodApp.Core.Models;
using FastFoodApp.Core.Services;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FastFoodApp.Client
{
    public partial class MainWindow : Window
    {
        private readonly LicenseService _licenseSvc = new LicenseService();
        private List<ItemPedido> _itemsPedido = new();
        private int _listaPrecionId = 1;
        private int _numeroPedidoActual = 1;
        private int _categoriaSeleccionadaId = 0; // 0 = Todas

        public MainWindow()
        {
            InitializeComponent();
            CargarListasDePrecios();
            LoadData();
            InitializePedidoSystem();
            CargarCategorias();
            CargarProductos();
        }

        private void LoadData()
        {
            var lic = _licenseSvc.GetLicense();
            TxtLicense.Text = lic.Type;
            TxtExpires.Text = lic.Expiry.HasValue ? $"{(lic.Expiry.Value - DateTime.Now).Days}d" : "";

            // Cargar el siguiente número de pedido
            using var ctx = new AppDbContext();
            var ultimoPedido = ctx.Orders.OrderByDescending(o => o.OrderNumber).FirstOrDefault();
            _numeroPedidoActual = ultimoPedido != null ? ultimoPedido.OrderNumber + 1 : 1;
            TxtNumeroPedido.Text = $"{_numeroPedidoActual:000}";

            RefrescarPedido();
        }

        private void InitializePedidoSystem()
        {
            this.PreviewKeyDown += MainWindow_PreviewKeyDown;

            // Iniciar reloj
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (s, e) =>
            {
                TxtHora.Text = DateTime.Now.ToString("HH:mm");
            };
            timer.Start();

            // Cargar estadísticas
            ActualizarEstadisticas();
        }

        private void ActualizarEstadisticas()
        {
            using var ctx = new AppDbContext();

            // Pedidos de hoy
            var hoy = DateTime.Today;
            var pedidosHoy = ctx.Orders.Count(o => o.Date.Date == hoy);
            TxtPedidosHoy.Text = pedidosHoy.ToString();

            // Pedidos pendientes/activos
            var pendientes = ctx.Orders.Count(o =>
                o.Status == "Pendiente" || o.Status == "En Preparación");
            TxtPendientes.Text = pendientes.ToString();
        }

        private void CargarListasDePrecios()
        {
            using var ctx = new AppDbContext();
            var listas = ctx.PriceLists
                .Where(pl => pl.IsActive)
                .OrderBy(pl => pl.Name)
                .ToList();

            CmbListaPrecios.ItemsSource = listas;

            if (listas.Count > 0)
            {
                CmbListaPrecios.SelectedIndex = 0;
                _listaPrecionId = listas[0].Id;
            }
        }

        private void CmbListaPrecios_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbListaPrecios.SelectedValue == null)
                return;

            var nuevaListaId = (int)CmbListaPrecios.SelectedValue;

            if (_itemsPedido.Count > 0 && nuevaListaId != _listaPrecionId)
            {
                var result = MessageBox.Show(
                    "Al cambiar la lista de precios se actualizarán los precios de los productos en el pedido actual.\n\n¿Desea continuar?",
                    "Confirmar cambio de lista",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    CmbListaPrecios.SelectedValue = _listaPrecionId;
                    return;
                }

                ActualizarPreciosEnPedido(nuevaListaId);
            }

            _listaPrecionId = nuevaListaId;
            CargarProductos(); // Recargar productos con nuevos precios
        }

        private void ActualizarPreciosEnPedido(int nuevaListaId)
        {
            using var ctx = new AppDbContext();

            foreach (var item in _itemsPedido)
            {
                var precioLista = ctx.ProductPriceLists
                    .FirstOrDefault(ppl => ppl.ProductId == item.ProductId && ppl.PriceListId == nuevaListaId);

                if (precioLista != null)
                {
                    item.Precio = precioLista.SalePrice;
                }
            }

            RefrescarPedido();
        }

        #region Carga de Categorías y Productos

        private void CargarCategorias()
        {
            PanelCategorias.Children.Clear();

            // Botón "Todas"
            var btnTodas = new Button
            {
                Content = "🍽️ Todas",
                Style = (Style)FindResource("CategoryButton"),
                Tag = _categoriaSeleccionadaId == 0 ? "Selected" : null
            };
            btnTodas.Click += (s, e) =>
            {
                _categoriaSeleccionadaId = 0;
                CargarCategorias();
                CargarProductos();
            };
            PanelCategorias.Children.Add(btnTodas);

            // Categorías desde BD
            using var ctx = new AppDbContext();
            var categorias = ctx.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToList();

            foreach (var cat in categorias)
            {
                var btn = new Button
                {
                    Content = GetCategoryIcon(cat.Name) + " " + cat.Name,
                    Style = (Style)FindResource("CategoryButton"),
                    Tag = _categoriaSeleccionadaId == cat.Id ? "Selected" : null
                };

                var catId = cat.Id;
                btn.Click += (s, e) =>
                {
                    _categoriaSeleccionadaId = catId;
                    CargarCategorias();
                    CargarProductos();
                };

                PanelCategorias.Children.Add(btn);
            }
        }

        private string GetCategoryIcon(string categoryName)
        {
            return categoryName.ToLower() switch
            {
                var n when n.Contains("hamburgues") => "🍔",
                var n when n.Contains("pizza") => "🍕",
                var n when n.Contains("papa") => "🍟",
                var n when n.Contains("bebida") => "🥤",
                var n when n.Contains("postre") => "🍰",
                var n when n.Contains("salsa") => "🧂",
                var n when n.Contains("combo") => "🎁",
                var n when n.Contains("ensalada") => "🥗",
                var n when n.Contains("sandwich") => "🥪",
                _ => "🍴"
            };
        }

        private void CargarProductos()
        {
            using var ctx = new AppDbContext();
            var productosViewModel = new List<ProductoCardViewModel>();

            // 1. CARGAR PRODUCTOS NORMALES
            IQueryable<Product> queryProductos = ctx.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive);

            if (_categoriaSeleccionadaId > 0)
            {
                queryProductos = queryProductos.Where(p => p.CategoryId == _categoriaSeleccionadaId);
            }

            var productos = queryProductos.ToList();

            foreach (var p in productos)
            {
                var precioLista = ctx.ProductPriceLists
                    .FirstOrDefault(ppl => ppl.ProductId == p.Id && ppl.PriceListId == _listaPrecionId);

                productosViewModel.Add(new ProductoCardViewModel
                {
                    ProductId = p.Id,
                    Name = p.Name,
                    ShortDescription = p.ShortDescription ?? "",
                    Price = precioLista?.SalePrice ?? 0,
                    TracksStock = p.TracksStock,
                    Stock = p.Stock ?? 0,
                    Icon = GetProductIcon(p.Category?.Name ?? ""),
                    LowStockVisibility = (p.TracksStock && p.Stock.HasValue && p.MinimumStock.HasValue && p.Stock.Value <= p.MinimumStock.Value)
                        ? Visibility.Visible
                        : Visibility.Collapsed,
                    IsCombo = false
                });
            }

            // 2. CARGAR COMBOS
            IQueryable<Combo> queryCombos = ctx.Combos
                .Include(c => c.Category)
                .Where(c => c.IsActive);

            if (_categoriaSeleccionadaId > 0)
            {
                queryCombos = queryCombos.Where(c => c.CategoryId == _categoriaSeleccionadaId);
            }

            var combos = queryCombos.ToList();

            foreach (var combo in combos)
            {
                productosViewModel.Add(new ProductoCardViewModel
                {
                    ComboId = combo.Id,
                    Name = combo.Name,
                    ShortDescription = combo.Description,
                    Price = combo.Price,
                    TracksStock = false, // Los combos no llevan stock propio
                    Stock = 0,
                    Icon = "🎁",
                    LowStockVisibility = Visibility.Collapsed,
                    IsCombo = true
                });
            }

            ProductosItemsControl.ItemsSource = productosViewModel.OrderBy(p => p.Name).ToList();
        }

        private string GetProductIcon(string categoryName)
        {
            return categoryName.ToLower() switch
            {
                var n when n.Contains("hamburgues") => "🍔",
                var n when n.Contains("pizza") => "🍕",
                var n when n.Contains("papa") => "🍟",
                var n when n.Contains("bebida") => "🥤",
                var n when n.Contains("postre") => "🍰",
                var n when n.Contains("combo") => "🎁",
                var n when n.Contains("ensalada") => "🥗",
                _ => "🍴"
            };
        }

        #endregion

        #region Manejo de Clicks en Products Cards

        private void ProductCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is ProductoCardViewModel producto)
            {
                if (producto.IsCombo)
                {
                    AgregarComboAlPedido(producto);
                }
                else
                {
                    AgregarProductoAlPedido(producto);
                }
            }
        }

        private void AgregarProductoAlPedido(ProductoCardViewModel producto)
        {
            // Solo validar stock si el producto lo requiere
            if (producto.TracksStock)
            {
                if (producto.Stock <= 0)
                {
                    MessageBox.Show($"No hay stock disponible de {producto.Name}.",
                        "Sin Stock",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var itemExistente = _itemsPedido.FirstOrDefault(i => i.ProductId == producto.ProductId);

                if (itemExistente != null)
                {
                    if (itemExistente.Cantidad + 1 > producto.Stock)
                    {
                        MessageBox.Show($"No hay suficiente stock de {producto.Name}.\nStock disponible: {producto.Stock}",
                            "Stock Insuficiente",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }
                    itemExistente.Cantidad++;
                }
                else
                {
                    _itemsPedido.Add(new ItemPedido
                    {
                        ProductId = producto.ProductId,
                        Name = producto.Name,
                        Cantidad = 1,
                        Precio = producto.Price,
                        StockDisponible = producto.Stock,
                        Customizaciones = new List<string>()
                    });
                }
            }
            else
            {
                // Producto sin control de stock (ej: hamburguesas)
                // Siempre se puede agregar
                var itemExistente = _itemsPedido.FirstOrDefault(i => i.ProductId == producto.ProductId);

                if (itemExistente != null)
                {
                    itemExistente.Cantidad++;
                }
                else
                {
                    _itemsPedido.Add(new ItemPedido
                    {
                        ProductId = producto.ProductId,
                        Name = producto.Name,
                        Cantidad = 1,
                        Precio = producto.Price,
                        StockDisponible = 9999, // Sin límite
                        Customizaciones = new List<string>()
                    });
                }
            }

            RefrescarPedido();
        }

        #endregion

        #region Manejo del Pedido

        private void RefrescarPedido()
        {
            foreach (var i in _itemsPedido)
                i.Subtotal = i.Cantidad * i.Precio;

            PedidoItemsControl.ItemsSource = null;
            PedidoItemsControl.ItemsSource = _itemsPedido;

            // Mostrar/ocultar mensaje de pedido vacío
            PedidoVacioPanel.Visibility = _itemsPedido.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            var total = _itemsPedido.Sum(i => i.Subtotal);
            TxtTotal.Text = total.ToString("C");

            BtnEnviarCocina.IsEnabled = _itemsPedido.Count > 0;
        }

        private void BtnEditarItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ItemPedido item)
            {
                var dialog = new CustomizationDialog(item.ProductId, item.Name);
                dialog.Owner = this;

                if (dialog.ShowDialog() == true)
                {
                    item.Customizaciones = dialog.CustomizacionesTexto
                        .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(c => c.Trim())
                        .ToList();

                    RefrescarPedido();
                }
            }
        }

        private void BtnEliminarItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ItemPedido item)
            {
                var result = MessageBox.Show(
                    $"¿Eliminar {item.Name} del pedido?",
                    "Confirmar",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _itemsPedido.Remove(item);
                    RefrescarPedido();
                }
            }
        }

        private void BtnEnviarCocina_Click(object sender, RoutedEventArgs e)
        {
            if (_itemsPedido.Count == 0)
                return;

            using var ctx = new AppDbContext();

            // ====================================================================
            // VALIDAR STOCK DE PRODUCTOS NORMALES (QUE LLEVAN CONTROL DE STOCK)
            // ====================================================================
            foreach (var item in _itemsPedido.Where(i => !i.IsCombo))
            {
                var prod = ctx.Products.FirstOrDefault(p => p.Id == item.ProductId);

                // Solo validar si el producto lleva control de stock
                if (prod != null && prod.TracksStock)
                {
                    if (!prod.Stock.HasValue || prod.Stock.Value < item.Cantidad)
                    {
                        MessageBox.Show(
                            $"❌ No hay stock suficiente de {item.Name}\n\n" +
                            $"Necesario: {item.Cantidad}\n" +
                            $"Disponible: {prod.Stock ?? 0}",
                            "Stock Insuficiente",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }
                }
            }

            // ====================================================================
            // VALIDAR STOCK DE PRODUCTOS EN COMBOS (SOLO LOS QUE LLEVAN STOCK)
            // ====================================================================
            var productosEnCombos = new Dictionary<int, int>(); // ProductId -> Cantidad total necesaria

            foreach (var item in _itemsPedido.Where(i => i.IsCombo))
            {
                var combo = ctx.Combos
                    .Include(c => c.Items)
                    .FirstOrDefault(c => c.Id == item.ComboId);

                if (combo != null)
                {
                    foreach (var comboItem in combo.Items)
                    {
                        // Obtener el producto seleccionado para este comboItem
                        int productoId = comboItem.ProductId;
                        if (item.ComboProductosSeleccionados.TryGetValue(comboItem.Id, out int productoSeleccionado))
                        {
                            productoId = productoSeleccionado;
                        }

                        // Verificar si el producto lleva control de stock antes de acumular
                        var producto = ctx.Products.FirstOrDefault(p => p.Id == productoId);
                        if (producto != null && producto.TracksStock)
                        {
                            // Acumular cantidad necesaria solo si lleva stock
                            if (productosEnCombos.ContainsKey(productoId))
                            {
                                productosEnCombos[productoId] += comboItem.Quantity * item.Cantidad;
                            }
                            else
                            {
                                productosEnCombos[productoId] = comboItem.Quantity * item.Cantidad;
                            }
                        }
                    }
                }
            }

            // Validar stock de productos de combos que sí llevan control
            foreach (var kvp in productosEnCombos)
            {
                var prod = ctx.Products.FirstOrDefault(p => p.Id == kvp.Key);

                if (prod != null && prod.TracksStock)
                {
                    if (!prod.Stock.HasValue || prod.Stock.Value < kvp.Value)
                    {
                        MessageBox.Show(
                            $"❌ No hay stock suficiente de {prod.Name} para los combos\n\n" +
                            $"Necesario: {kvp.Value}\n" +
                            $"Disponible: {prod.Stock ?? 0}",
                            "Stock Insuficiente en Combo",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }
                }
            }

            var total = _itemsPedido.Sum(i => i.Subtotal);

            // ====================================================================
            // CREAR EL PEDIDO
            // ====================================================================
            var pedido = new Order
            {
                OrderNumber = _numeroPedidoActual,
                Date = DateTime.Now,
                CustomerName = string.IsNullOrWhiteSpace(TxtCliente.Text) ? "Cliente" : TxtCliente.Text,
                CustomerPhone = TxtTelefono.Text,
                Status = "Pendiente",
                Total = total,
                Items = new List<OrderItem>()
            };

            // Agregar items normales
            foreach (var item in _itemsPedido.Where(i => !i.IsCombo))
            {
                pedido.Items.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Cantidad,
                    UnitPrice = item.Precio,
                    Subtotal = item.Subtotal,
                    Customizations = item.Customizaciones.Count > 0 ? string.Join(";", item.Customizaciones) : null
                });
            }

            // Agregar items de combos (como texto descriptivo)
            foreach (var item in _itemsPedido.Where(i => i.IsCombo))
            {
                pedido.Items.Add(new OrderItem
                {
                    ProductId = null, // Los combos no tienen ProductId directo
                    Quantity = item.Cantidad,
                    UnitPrice = item.Precio,
                    Subtotal = item.Subtotal,
                    Customizations = $"COMBO: {item.Name} - {string.Join(", ", item.Customizaciones)}"
                });
            }

            ctx.Orders.Add(pedido);

            // ====================================================================
            // DESCONTAR STOCK (SOLO PRODUCTOS QUE LLEVAN CONTROL)
            // ====================================================================
            var usuario = Environment.UserName;

            // Stock de productos normales
            foreach (var item in _itemsPedido.Where(i => !i.IsCombo))
            {
                var prod = ctx.Products.FirstOrDefault(p => p.Id == item.ProductId);

                if (prod != null && prod.TracksStock && prod.Stock.HasValue)
                {
                    prod.Stock = prod.Stock.Value - item.Cantidad;

                    ctx.StockMovements.Add(new StockMovement
                    {
                        ProductId = item.ProductId,
                        SupplierId = null,
                        MovementType = "Pedido",
                        Quantity = -item.Cantidad,
                        Cost = null,
                        Reason = $"Pedido #{_numeroPedidoActual} - {TxtCliente.Text}",
                        Date = DateTime.Now,
                        User = usuario
                    });
                }
            }

            // Stock de productos en combos (solo los que llevan stock)
            foreach (var kvp in productosEnCombos)
            {
                var prod = ctx.Products.FirstOrDefault(p => p.Id == kvp.Key);

                if (prod != null && prod.TracksStock && prod.Stock.HasValue)
                {
                    prod.Stock = prod.Stock.Value - kvp.Value;

                    ctx.StockMovements.Add(new StockMovement
                    {
                        ProductId = kvp.Key,
                        SupplierId = null,
                        MovementType = "Pedido (Combo)",
                        Quantity = -kvp.Value,
                        Cost = null,
                        Reason = $"Pedido #{_numeroPedidoActual} - Combo - {TxtCliente.Text}",
                        Date = DateTime.Now,
                        User = usuario
                    });
                }
            }

            ctx.SaveChanges();
            ActualizarEstadisticas();

            MessageBox.Show(
                $"✅ Pedido #{_numeroPedidoActual} enviado a cocina\n\n" +
                $"Cliente: {pedido.CustomerName}\n" +
                $"Total: {total:C}\n" +
                $"Items: {_itemsPedido.Count}\n\n" +
                $"El pedido está listo para prepararse.",
                "Pedido Confirmado",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            // Nuevo pedido
            BtnNuevoPedido_Click(sender, e);
        }

        private void BtnNuevoPedido_Click(object sender, RoutedEventArgs e)
        {
            if (_itemsPedido.Count > 0)
            {
                var result = MessageBox.Show(
                    "¿Desea iniciar un nuevo pedido? Se perderá el pedido actual.",
                    "Nuevo Pedido",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                    return;
            }

            _itemsPedido.Clear();
            RefrescarPedido();

            TxtCliente.Text = "Cliente";
            TxtTelefono.Clear();

            // Actualizar número de pedido
            using var ctx = new AppDbContext();
            var ultimoPedido = ctx.Orders.OrderByDescending(o => o.OrderNumber).FirstOrDefault();
            _numeroPedidoActual = ultimoPedido != null ? ultimoPedido.OrderNumber + 1 : 1;
            TxtNumeroPedido.Text = $"{_numeroPedidoActual:000}";
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            if (_itemsPedido.Count > 0)
            {
                var result = MessageBox.Show(
                    "¿Está seguro que desea cancelar el pedido actual?",
                    "Cancelar Pedido",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _itemsPedido.Clear();
                    RefrescarPedido();
                    MessageBox.Show("Pedido cancelado", "Información", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        #endregion

        #region Eventos del Menú

        private void BtnVerCocina_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Pantalla de cocina en desarrollo", "Cocina", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnPedidosHoy_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Lista de pedidos del día en desarrollo", "Pedidos", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnHistorialVentas_Click(object sender, RoutedEventArgs e)
        {
            var window = new SalesHistoryWindow();
            window.ShowDialog();
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
                CargarListasDePrecios();
            }
        }

        private void BtnNuevoArticulo_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new ProductDialog();
            if (dlg.ShowDialog() == true)
            {
                CargarProductos();
            }
        }

        private void BtnEditarArticulo_Click(object sender, RoutedEventArgs e)
        {
            var win = new ProductsWindow();
            win.ShowDialog();
            CargarProductos();
        }

        private void BtnVerArticulos_Click(object sender, RoutedEventArgs e)
        {
            var win = new ProductsWindow();
            win.ShowDialog();
            CargarProductos();
        }

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
                CargarProductos();
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
            CargarProductos();
        }

        private void MenuActualizacionMasivaPrecios_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new PriceUpdateDialog();
            dialog.ShowDialog();
            CargarProductos();
        }

        private void BtnVerInsumos_Click(object sender, RoutedEventArgs e)
        {
            var window = new RawMaterialsWindow();
            window.ShowDialog();
        }

        private void BtnNuevoInsumo_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new RawMaterialDialog();
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                MessageBox.Show("✓ Insumo creado correctamente.", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        #endregion

        #region Atajos de Teclado

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // F1 para enviar a cocina
            if (e.Key == Key.F1)
            {
                if (_itemsPedido.Count > 0)
                    BtnEnviarCocina_Click(sender, e);
                e.Handled = true;
            }

            // F2 para nuevo pedido
            if (e.Key == Key.F2)
            {
                BtnNuevoPedido_Click(sender, e);
                e.Handled = true;
            }
        }

        private void AgregarComboAlPedido(ProductoCardViewModel combo)
        {
            // Abrir diálogo de personalización del combo
            var dialog = new ComboSelectionDialog(combo.ComboId);
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                using var ctx = new AppDbContext();

                // Obtener el combo completo
                var comboDb = ctx.Combos
                    .Include(c => c.Items)
                        .ThenInclude(i => i.Product)
                    .FirstOrDefault(c => c.Id == combo.ComboId);

                if (comboDb == null) return;

                // Crear un item de pedido para el combo
                var itemPedido = new ItemPedido
                {
                    ProductId = 0, // Los combos no tienen ProductId
                    ComboId = combo.ComboId,
                    IsCombo = true,
                    Name = combo.Name,
                    Cantidad = 1,
                    Precio = dialog.PrecioCombo,
                    StockDisponible = 999,
                    Customizaciones = new List<string>()
                };

                // Construir descripción del combo con productos seleccionados
                var descripcionCombo = new List<string>();
                foreach (var item in comboDb.Items)
                {
                    if (dialog.ProductosSeleccionados.TryGetValue(item.Id, out int productoSeleccionadoId))
                    {
                        var producto = ctx.Products.FirstOrDefault(p => p.Id == productoSeleccionadoId);
                        if (producto != null)
                        {
                            var cantidadText = item.Quantity > 1 ? $"{item.Quantity}x " : "";
                            descripcionCombo.Add($"{cantidadText}{producto.Name}");
                        }
                    }
                }

                itemPedido.Customizaciones = descripcionCombo;

                // Guardar los productos seleccionados para descontar stock después
                itemPedido.ComboProductosSeleccionados = dialog.ProductosSeleccionados;

                _itemsPedido.Add(itemPedido);
                RefrescarPedido();

                // Mostrar confirmación
                MessageBox.Show(
                    $"✅ {combo.Name} agregado al pedido\n\n" +
                    $"Incluye:\n" + string.Join("\n", descripcionCombo),
                    "Combo Agregado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void BtnVerCombos_Click(object sender, RoutedEventArgs e)
        {
            var window = new CombosWindow();
            window.ShowDialog();
            CargarProductos(); // Refrescar por si hubo cambios
        }

        private void BtnNuevoCombo_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ComboEditorDialog();
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                CargarProductos();
                MessageBox.Show("✓ Combo creado correctamente", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        #endregion
    }

    #region ViewModels

    /// <summary>
    /// ViewModel para mostrar productos y combos en cards visuales
    /// </summary>
    public class ProductoCardViewModel
    {
        public int ProductId { get; set; }
        public int ComboId { get; set; }
        public bool IsCombo { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ShortDescription { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool TracksStock { get; set; }  // NUEVO
        public int Stock { get; set; }
        public string Icon { get; set; } = "🍴";
        public Visibility LowStockVisibility { get; set; } = Visibility.Collapsed;
    }

    /// <summary>
    /// Clase para representar un ítem en el pedido actual
    /// </summary>
    public class ItemPedido
    {
        public int ProductId { get; set; }
        public int ComboId { get; set; }
        public bool IsCombo { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
        public decimal Subtotal { get; set; }
        public int StockDisponible { get; set; }
        public List<string> Customizaciones { get; set; } = new();

        // NUEVO: Para combos, guardar qué productos fueron seleccionados
        public Dictionary<int, int> ComboProductosSeleccionados { get; set; } = new();

        /// <summary>
        /// Texto formateado de las customizaciones para mostrar en el grid
        /// </summary>
        public string CustomizacionesTexto
        {
            get
            {
                if (IsCombo)
                {
                    return Customizaciones.Count > 0
                        ? "🎁 " + string.Join(" • ", Customizaciones)
                        : "";
                }
                else
                {
                    return Customizaciones.Count > 0
                        ? string.Join(", ", Customizaciones)
                        : "";
                }
            }
        }

        public Visibility HasCustomizations => Customizaciones.Count > 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    #endregion
}