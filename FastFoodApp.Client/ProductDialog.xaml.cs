using FastFoodApp.Core.Data;
using FastFoodApp.Core.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FastFoodApp.Client
{
    // Clase auxiliar para manejar los precios por lista
    public class PrecioLista : INotifyPropertyChanged
    {
        public int ListaId { get; set; }
        public string NombreLista { get; set; } = "";

        private decimal _costo;
        public decimal Costo
        {
            get => _costo;
            set
            {
                if (_costo != value)
                {
                    _costo = value;
                    OnPropertyChanged(nameof(Costo));
                    CalcularPrecioVenta();
                }
            }
        }

        public string IVA { get; set; } = "21%";

        private decimal _porcentajeUtilidad;
        public decimal PorcentajeUtilidad
        {
            get => _porcentajeUtilidad;
            set
            {
                if (_porcentajeUtilidad != value)
                {
                    _porcentajeUtilidad = value;
                    OnPropertyChanged(nameof(PorcentajeUtilidad));
                    CalcularPrecioVenta();
                }
            }
        }

        private decimal _precioVenta;
        public decimal PrecioVenta
        {
            get => _precioVenta;
            set
            {
                if (_precioVenta != value)
                {
                    _precioVenta = value;
                    OnPropertyChanged(nameof(PrecioVenta));
                }
            }
        }

        public decimal PorcentajeImpInt { get; set; }
        public DateTime UltimaActualizacion { get; set; }

        private void CalcularPrecioVenta()
        {
            if (Costo > 0 && PorcentajeUtilidad >= 0)
            {
                PrecioVenta = Math.Round(Costo + (Costo * PorcentajeUtilidad / 100), 2);
            }
            else
            {
                PrecioVenta = 0;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Clase auxiliar para manejar los ingredientes de la receta en la UI
    public class ProductRecipeViewModel : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public int RawMaterialId { get; set; }
        public string RawMaterialCode { get; set; } = string.Empty;
        public string RawMaterialName { get; set; } = string.Empty;

        private decimal _quantity;
        public decimal Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged(nameof(Quantity));
                }
            }
        }

        public string Unit { get; set; } = "UN";
        public decimal AvailableQuantity { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Clase auxiliar para manejar los proveedores del producto
    public class ProductSupplierViewModel
    {
        public int Numero { get; set; }
        public int SupplierId { get; set; }
        public string NombreProveedor { get; set; } = string.Empty;
        public decimal Costo { get; set; }
        public DateTime UltimaActualizacion { get; set; }
        public bool EsPredeterminado { get; set; }
        public string TextoPredeterminado => EsPredeterminado ? "✓ Predeterminado" : "Establecer";
    }

    public partial class ProductDialog : Window
    {
        private readonly Product? _product;
        private readonly bool _isReadOnly;
        private readonly bool _isNewProduct;
        private ObservableCollection<PrecioLista> _listasPrecios;
        private ObservableCollection<ProductSupplierViewModel> _proveedores;
        private ObservableCollection<ProductRecipeViewModel> _receta;

        public ProductDialog(Product? product = null, bool isReadOnly = false)
        {
            InitializeComponent();

            _isReadOnly = isReadOnly;
            _isNewProduct = product == null;
            _listasPrecios = new ObservableCollection<PrecioLista>();
            _proveedores = new ObservableCollection<ProductSupplierViewModel>();
            _receta = new ObservableCollection<ProductRecipeViewModel>(); // ← NUEVO


            // Cargar categorías desde la base de datos
            CargarCategorias();

            if (product != null)
            {
                using var ctx = new AppDbContext();
                _product = ctx.Products.FirstOrDefault(p => p.Id == product.Id);

                if (_product != null)
                {
                    LoadProductData();
                    TxtTituloVentana.Text = _isReadOnly ? "VER PRODUCTO" : "EDITAR PRODUCTO";
                    Title = _isReadOnly ? "Ver Producto" : "Editar Producto";
                }
            }
            else
            {
                _product = new Product();
                TxtTituloVentana.Text = "NUEVO PRODUCTO";
                Title = "Nuevo Producto";
                TxtObservaciones.Text = "";
            }

            if (_isReadOnly)
            {
                SetControlsReadOnly();
                BtnGuardar.Visibility = Visibility.Collapsed;
                BtnCancelar.Content = "✖ Cerrar";
            }

            // Cargar las listas de precios y proveedores
            CargarListasPrecios();
            CargarProveedores();
            CargarReceta(); // ← NUEVO

        }

        private void CargarCategorias()
        {
            using var ctx = new AppDbContext();
            var categorias = ctx.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToList();

            CmbCategoria.ItemsSource = categorias;
        }

        private void CargarListasPrecios()
        {
            _listasPrecios.Clear();

            using var ctx = new AppDbContext();

            var listasDelSistema = ctx.PriceLists
                .Where(pl => pl.IsActive)
                .OrderBy(pl => pl.Name)
                .ToList();

            if (_isNewProduct || _product == null)
            {
                foreach (var lista in listasDelSistema)
                {
                    _listasPrecios.Add(new PrecioLista
                    {
                        ListaId = lista.Id,
                        NombreLista = lista.Name,
                        Costo = 0m,
                        IVA = "21%",
                        PorcentajeUtilidad = 0m,
                        PrecioVenta = 0m,
                        PorcentajeImpInt = 0m,
                        UltimaActualizacion = DateTime.Now
                    });
                }
            }
            else
            {
                var preciosGuardados = ctx.ProductPriceLists
                    .Where(ppl => ppl.ProductId == _product.Id)
                    .ToDictionary(ppl => ppl.PriceListId);

                foreach (var lista in listasDelSistema)
                {
                    if (preciosGuardados.ContainsKey(lista.Id))
                    {
                        var precio = preciosGuardados[lista.Id];
                        _listasPrecios.Add(new PrecioLista
                        {
                            ListaId = lista.Id,
                            NombreLista = lista.Name,
                            Costo = precio.CostPrice,
                            IVA = precio.IVA,
                            PorcentajeUtilidad = precio.ProfitPercentage,
                            PrecioVenta = precio.SalePrice,
                            PorcentajeImpInt = precio.InternalTaxPercentage,
                            UltimaActualizacion = precio.LastUpdate
                        });
                    }
                    else
                    {
                        _listasPrecios.Add(new PrecioLista
                        {
                            ListaId = lista.Id,
                            NombreLista = lista.Name,
                            Costo = 0m,
                            IVA = "21%",
                            PorcentajeUtilidad = 0m,
                            PrecioVenta = 0m,
                            PorcentajeImpInt = 0m,
                            UltimaActualizacion = DateTime.Now
                        });
                    }
                }
            }

            DgPrecios.ItemsSource = _listasPrecios;
        }

        private void CargarProveedores()
        {
            _proveedores.Clear();

            if (_product != null && !_isNewProduct)
            {
                using var ctx = new AppDbContext();
                var productSuppliers = ctx.ProductSuppliers
                    .Where(ps => ps.ProductId == _product.Id)
                    .Join(ctx.Suppliers,
                        ps => ps.SupplierId,
                        s => s.Id,
                        (ps, s) => new ProductSupplierViewModel
                        {
                            Numero = ps.Id,
                            SupplierId = s.Id,
                            NombreProveedor = s.Name,
                            Costo = ps.Cost,
                            UltimaActualizacion = ps.LastUpdate,
                            EsPredeterminado = ps.IsDefault
                        })
                    .ToList();

                foreach (var proveedor in productSuppliers)
                {
                    _proveedores.Add(proveedor);
                }
            }

            DgProveedores.ItemsSource = _proveedores;
            ActualizarContadorProveedores();
        }

        private void ActualizarContadorProveedores()
        {
            TxtContadorProveedores.Text = $"{_proveedores.Count} / 10 proveedores";
        }

        private void LoadProductData()
        {
            if (_product == null) return;

            TxtCode.Text = _product.Code ?? "";
            TxtDescription.Text = _product.Name ?? "";
            TxtObservaciones.Text = _product.Observations ?? "";
            TxtDescripcionBreve.Text = _product.ShortDescription ?? "";

            // Cargar Stock
            ChkLlevaStock.IsChecked = _product.TracksStock;
            TxtStockActual.Text = _product.Stock?.ToString() ?? "0";
            TxtStockMinimo.Text = _product.MinimumStock?.ToString() ?? "0";
            TxtStockMaximo.Text = _product.MaximumStock?.ToString() ?? "0";

            // Cargar información de vencimiento
            ChkLlevaVencimiento.IsChecked = _product.HasExpiry;
            DpFechaVencimiento.SelectedDate = _product.ExpiryDate;
            TxtAlertaVencimiento.Text = _product.ExpiryAlertDays?.ToString() ?? "0";

            // Cargar Categoría
            if (_product.CategoryId.HasValue)
            {
                CmbCategoria.SelectedValue = _product.CategoryId.Value;
            }

            // Cargar imagen si existe
            if (!string.IsNullOrEmpty(_product.ImagePath) && System.IO.File.Exists(_product.ImagePath))
            {
                try
                {
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(_product.ImagePath);
                    bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    ImgProducto.Source = bitmap;
                    PlaceholderImagen.Visibility = Visibility.Collapsed;
                    ImgProducto.Visibility = Visibility.Visible;
                }
                catch
                {
                    ImgProducto.Visibility = Visibility.Collapsed;
                    PlaceholderImagen.Visibility = Visibility.Visible;
                }
            }

            ChkUsaReceta.IsChecked = _product.HasRecipe; // ← NUEVO

            UpdateStockStatus();
        }

        private void SetControlsReadOnly()
        {
            TxtCode.IsReadOnly = true;
            TxtDescription.IsReadOnly = true;
            TxtObservaciones.IsReadOnly = true;
            TxtDescripcionBreve.IsReadOnly = true;
            TxtStockMinimo.IsReadOnly = true;
            TxtStockMaximo.IsReadOnly = true;
            CmbCategoria.IsEnabled = false;
            ChkLlevaStock.IsEnabled = false;
            ChkLlevaVencimiento.IsEnabled = false;
            DpFechaVencimiento.IsEnabled = false;
            TxtAlertaVencimiento.IsReadOnly = true;

            var grayBrush = Brushes.WhiteSmoke;
            TxtCode.Background = grayBrush;
            TxtDescription.Background = grayBrush;
            TxtObservaciones.Background = grayBrush;
            TxtDescripcionBreve.Background = grayBrush;
            TxtStockMinimo.Background = grayBrush;
            TxtStockMaximo.Background = grayBrush;
            TxtAlertaVencimiento.Background = grayBrush;
        }

        private void ChkLlevaStock_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool llevaStock = ChkLlevaStock.IsChecked == true;

            PanelStock.IsEnabled = llevaStock;
            MensajeSinStock.Visibility = llevaStock ? Visibility.Collapsed : Visibility.Visible;

            if (!llevaStock)
            {
                TxtStockActual.Text = "0";
                TxtStockMinimo.Text = "0";
                TxtStockMaximo.Text = "0";
            }

            UpdateStockStatus();
        }

        private void UpdateStockStatus()
        {
            if (BorderEstadoStock == null || TxtEstadoStock == null)
            {
                return;
            }

            if (!ChkLlevaStock.IsChecked == true)
            {
                BorderEstadoStock.Background = new SolidColorBrush(Color.FromRgb(248, 249, 250));
                BorderEstadoStock.BorderBrush = new SolidColorBrush(Color.FromRgb(204, 204, 204));
                TxtEstadoStock.Text = "Configura los valores de stock para recibir alertas.";
                TxtEstadoStock.Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102));
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtStockActual?.Text) ||
                string.IsNullOrWhiteSpace(TxtStockMinimo?.Text) ||
                string.IsNullOrWhiteSpace(TxtStockMaximo?.Text))
            {
                return;
            }

            if (!int.TryParse(TxtStockActual.Text, out var stockActual))
            {
                return;
            }

            int.TryParse(TxtStockMinimo.Text, out var stockMinimo);
            int.TryParse(TxtStockMaximo.Text, out var stockMaximo);

            if (stockMinimo > 0 && stockActual < stockMinimo)
            {
                BorderEstadoStock.Background = new SolidColorBrush(Color.FromRgb(255, 235, 230));
                BorderEstadoStock.BorderBrush = new SolidColorBrush(Color.FromRgb(230, 126, 34));
                TxtEstadoStock.Text = $"⚠️ ALERTA: El stock actual ({stockActual}) está por debajo del mínimo ({stockMinimo}).";
                TxtEstadoStock.Foreground = new SolidColorBrush(Color.FromRgb(230, 126, 34));
            }
            else if (stockMaximo > 0 && stockActual > stockMaximo)
            {
                BorderEstadoStock.Background = new SolidColorBrush(Color.FromRgb(230, 245, 255));
                BorderEstadoStock.BorderBrush = new SolidColorBrush(Color.FromRgb(52, 152, 219));
                TxtEstadoStock.Text = $"ℹ️ INFORMACIÓN: El stock actual ({stockActual}) supera el máximo ({stockMaximo}).";
                TxtEstadoStock.Foreground = new SolidColorBrush(Color.FromRgb(52, 152, 219));
            }
            else if (stockMinimo > 0 || stockMaximo > 0)
            {
                BorderEstadoStock.Background = new SolidColorBrush(Color.FromRgb(230, 255, 237));
                BorderEstadoStock.BorderBrush = new SolidColorBrush(Color.FromRgb(39, 174, 96));
                TxtEstadoStock.Text = $"✓ Stock en rango normal ({stockActual} unidades).";
                TxtEstadoStock.Foreground = new SolidColorBrush(Color.FromRgb(39, 174, 96));
            }
            else
            {
                BorderEstadoStock.Background = new SolidColorBrush(Color.FromRgb(248, 249, 250));
                BorderEstadoStock.BorderBrush = new SolidColorBrush(Color.FromRgb(204, 204, 204));
                TxtEstadoStock.Text = "Configura los valores de stock mínimo y máximo para recibir alertas.";
                TxtEstadoStock.Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102));
            }
        }

        private void TxtStock_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateStockStatus();
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtCode.Text))
            {
                MessageBox.Show("El código es obligatorio.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtCode.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtDescription.Text))
            {
                MessageBox.Show("La descripción es obligatoria.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtDescription.Focus();
                return;
            }

            // Validación: Si tiene control de vencimiento, debe tener fecha
            if (ChkLlevaVencimiento.IsChecked == true && DpFechaVencimiento.SelectedDate == null)
            {
                MessageBox.Show("Debe establecer una fecha de vencimiento si el producto lleva control de vencimiento.",
                    "Validación",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                MainTabControl.SelectedIndex = 4; // Tab Vencimiento
                DpFechaVencimiento.Focus();
                return;
            }

            try
            {
                using var ctx = new AppDbContext();

                if (_isNewProduct)
                {
                    if (ctx.Products.Any(p => p.Code == TxtCode.Text.Trim()))
                    {
                        MessageBox.Show("Ya existe un producto con ese código.", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var nuevoProducto = new Product
                    {
                        Code = TxtCode.Text.Trim(),
                        Name = TxtDescription.Text.Trim(),
                        Observations = TxtObservaciones.Text.Trim(),
                        CategoryId = CmbCategoria.SelectedValue as int?,
                        UnitOfSale = "UN", // Siempre unidad
                        TracksStock = ChkLlevaStock.IsChecked == true,
                        Stock = ChkLlevaStock.IsChecked == true ? 0 : (int?)null,
                        MinimumStock = ChkLlevaStock.IsChecked == true && int.TryParse(TxtStockMinimo.Text, out var minStock) ? minStock : (int?)null,
                        MaximumStock = ChkLlevaStock.IsChecked == true && int.TryParse(TxtStockMaximo.Text, out var maxStock) ? maxStock : (int?)null,
                        IsActive = true,
                        ImagePath = _product?.ImagePath,
                        CreatedAt = DateTime.Now,
                        LastPriceUpdate = DateTime.Now,
                        HasExpiry = ChkLlevaVencimiento.IsChecked == true,
                        ExpiryDate = ChkLlevaVencimiento.IsChecked == true ? DpFechaVencimiento.SelectedDate : null,
                        ExpiryAlertDays = ChkLlevaVencimiento.IsChecked == true && int.TryParse(TxtAlertaVencimiento.Text, out var alertDays) ? alertDays : (int?)null,
                        ShortDescription = TxtDescripcionBreve.Text.Trim(),
                        HasRecipe = ChkUsaReceta.IsChecked == true, // ← NUEVO
                    };

                    ctx.Products.Add(nuevoProducto);
                }
                else
                {
                    if (_product == null) return;

                    var existing = ctx.Products.FirstOrDefault(p => p.Id == _product.Id);
                    if (existing != null)
                    {
                        existing.Code = TxtCode.Text.Trim();
                        existing.Name = TxtDescription.Text.Trim();
                        existing.Observations = TxtObservaciones.Text.Trim();
                        existing.CategoryId = CmbCategoria.SelectedValue as int?;
                        existing.UnitOfSale = "UN"; // Siempre unidad
                        existing.TracksStock = ChkLlevaStock.IsChecked == true;
                        existing.Stock = ChkLlevaStock.IsChecked == true ? existing.Stock : null;
                        existing.MinimumStock = ChkLlevaStock.IsChecked == true && int.TryParse(TxtStockMinimo.Text, out var minStock) ? minStock : (int?)null;
                        existing.MaximumStock = ChkLlevaStock.IsChecked == true && int.TryParse(TxtStockMaximo.Text, out var maxStock) ? maxStock : (int?)null;
                        existing.ImagePath = _product.ImagePath;
                        existing.UpdatedAt = DateTime.Now;
                        existing.HasExpiry = ChkLlevaVencimiento.IsChecked == true;
                        existing.ExpiryDate = ChkLlevaVencimiento.IsChecked == true ? DpFechaVencimiento.SelectedDate : null;
                        existing.ExpiryAlertDays = ChkLlevaVencimiento.IsChecked == true && int.TryParse(TxtAlertaVencimiento.Text, out var alertDays) ? alertDays : (int?)null;
                        existing.ShortDescription = TxtDescripcionBreve.Text.Trim();
                        existing.HasRecipe = ChkUsaReceta.IsChecked == true; // ← NUEVO
                    }
                }

                ctx.SaveChanges();

                MessageBox.Show("✓ Producto guardado correctamente.", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                MessageBox.Show($"Error al guardar el producto:\n{innerMessage}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            if (!_isReadOnly && !_isNewProduct)
            {
                var result = MessageBox.Show("¿Desea descartar los cambios?", "Confirmar",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                    return;
            }

            DialogResult = false;
            Close();
        }

        private void BtnCargarImagen_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Seleccionar imagen del producto",
                Filter = "Archivos de imagen|*.jpg;*.jpeg;*.png;*.bmp;*.gif|Todos los archivos|*.*",
                FilterIndex = 1
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string? rutaImagenAnterior = _product?.ImagePath;

                    if (_product != null)
                    {
                        _product.ImagePath = openFileDialog.FileName;
                    }

                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(openFileDialog.FileName);
                    bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    ImgProducto.Source = bitmap;
                    PlaceholderImagen.Visibility = Visibility.Collapsed;
                    ImgProducto.Visibility = Visibility.Visible;

                    if (!string.IsNullOrEmpty(rutaImagenAnterior) &&
                        System.IO.File.Exists(rutaImagenAnterior) &&
                        rutaImagenAnterior != openFileDialog.FileName)
                    {
                        System.IO.File.Delete(rutaImagenAnterior);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cargar la imagen:\n{ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnEliminarImagen_Click(object sender, RoutedEventArgs e)
        {
            if (_product == null || string.IsNullOrEmpty(_product.ImagePath))
            {
                MessageBox.Show("No hay ninguna imagen cargada para eliminar.", "Información",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show("¿Está seguro que desea eliminar la imagen del producto?",
                "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    string? rutaImagenAnterior = _product.ImagePath;
                    _product.ImagePath = null;

                    ImgProducto.Source = null;
                    ImgProducto.Visibility = Visibility.Collapsed;
                    PlaceholderImagen.Visibility = Visibility.Visible;

                    if (!_isNewProduct)
                    {
                        using var ctx = new AppDbContext();
                        var existing = ctx.Products.FirstOrDefault(p => p.Id == _product.Id);
                        if (existing != null)
                        {
                            existing.ImagePath = null;
                            ctx.SaveChanges();
                        }
                    }

                    if (!string.IsNullOrEmpty(rutaImagenAnterior) && System.IO.File.Exists(rutaImagenAnterior))
                    {
                        System.IO.File.Delete(rutaImagenAnterior);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al eliminar la imagen:\n{ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnModificar_Click(object sender, RoutedEventArgs e)
        {
            if (_product == null || _isNewProduct)
            {
                MessageBox.Show("Debe guardar el producto antes de modificar los precios.", "Información",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                using var ctx = new AppDbContext();

                foreach (var precioLista in _listasPrecios)
                {
                    var precioExistente = ctx.ProductPriceLists
                        .FirstOrDefault(ppl => ppl.ProductId == _product.Id && ppl.PriceListId == precioLista.ListaId);

                    if (precioExistente != null)
                    {
                        precioExistente.CostPrice = precioLista.Costo;
                        precioExistente.SalePrice = precioLista.PrecioVenta;
                        precioExistente.ProfitPercentage = precioLista.PorcentajeUtilidad;
                        precioExistente.InternalTaxPercentage = precioLista.PorcentajeImpInt;
                        precioExistente.IVA = precioLista.IVA;
                        precioExistente.LastUpdate = DateTime.Now;
                    }
                    else if (precioLista.Costo > 0 || precioLista.PrecioVenta > 0)
                    {
                        ctx.ProductPriceLists.Add(new ProductPriceList
                        {
                            ProductId = _product.Id,
                            PriceListId = precioLista.ListaId,
                            CostPrice = precioLista.Costo,
                            SalePrice = precioLista.PrecioVenta,
                            ProfitPercentage = precioLista.PorcentajeUtilidad,
                            InternalTaxPercentage = precioLista.PorcentajeImpInt,
                            IVA = precioLista.IVA,
                            LastUpdate = DateTime.Now
                        });
                    }

                    precioLista.UltimaActualizacion = DateTime.Now;
                }

                ctx.SaveChanges();

                MessageBox.Show("✓ Precios actualizados correctamente.", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                CargarListasPrecios();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al actualizar precios:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAgregarProveedor_Click(object sender, RoutedEventArgs e)
        {
            if (_product == null || _isNewProduct)
            {
                MessageBox.Show("Debe guardar el producto antes de agregar proveedores.", "Información",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_proveedores.Count >= 10)
            {
                MessageBox.Show("Ha alcanzado el límite máximo de 10 proveedores por producto.", "Límite alcanzado",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new SupplierSelectionDialog(_product.Id);
            if (dialog.ShowDialog() == true && dialog.SelectedSupplier != null)
            {
                var nuevoProveedor = new ProductSupplierViewModel
                {
                    Numero = _proveedores.Count + 1,
                    SupplierId = dialog.SelectedSupplier.SupplierId,
                    NombreProveedor = dialog.SelectedSupplier.NombreProveedor,
                    Costo = dialog.SelectedSupplier.Costo,
                    UltimaActualizacion = DateTime.Now,
                    EsPredeterminado = _proveedores.Count == 0
                };

                _proveedores.Add(nuevoProveedor);
                ActualizarContadorProveedores();
            }
        }

        private void BtnGuardarProveedores_Click(object sender, RoutedEventArgs e)
        {
            if (_product == null || _isNewProduct)
            {
                MessageBox.Show("Debe guardar el producto antes de guardar proveedores.", "Información",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
            }

            try
            {
                using var ctx = new AppDbContext();

                // Eliminar proveedores existentes
                var existentes = ctx.ProductSuppliers.Where(ps => ps.ProductId == _product.Id).ToList();
                ctx.ProductSuppliers.RemoveRange(existentes);

                // Agregar los nuevos proveedores
                foreach (var proveedor in _proveedores)
                {
                    ctx.ProductSuppliers.Add(new ProductSupplier
                    {
                        ProductId = _product.Id,
                        SupplierId = proveedor.SupplierId,
                        Cost = proveedor.Costo,
                        IsDefault = proveedor.EsPredeterminado,
                        LastUpdate = DateTime.Now
                    });
                }

                ctx.SaveChanges();

                MessageBox.Show("✓ Proveedores guardados correctamente.", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar proveedores:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnPredeterminado_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var proveedor = button?.Tag as ProductSupplierViewModel;

            if (proveedor != null)
            {
                // Quitar el predeterminado de todos
                foreach (var p in _proveedores)
                {
                    p.EsPredeterminado = false;
                }

                // Establecer este como predeterminado
                proveedor.EsPredeterminado = true;

                // Refrescar la grilla
                DgProveedores.Items.Refresh();
            }
        }

        private void BtnQuitarProveedor_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var proveedor = button?.Tag as ProductSupplierViewModel;

            if (proveedor != null)
            {
                var result = MessageBox.Show(
                    $"¿Está seguro que desea quitar al proveedor '{proveedor.NombreProveedor}'?",
                    "Confirmar",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _proveedores.Remove(proveedor);

                    // Renumerar
                    int numero = 1;
                    foreach (var p in _proveedores)
                    {
                        p.Numero = numero++;
                    }

                    // Si se quitó el predeterminado y hay más proveedores, hacer el primero predeterminado
                    if (proveedor.EsPredeterminado && _proveedores.Count > 0)
                    {
                        _proveedores[0].EsPredeterminado = true;
                    }

                    ActualizarContadorProveedores();
                    DgProveedores.Items.Refresh();
                }
            }
        }

        private void ChkLlevaVencimiento_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool llevaVencimiento = ChkLlevaVencimiento.IsChecked == true;

            DpFechaVencimiento.IsEnabled = llevaVencimiento;
            TxtAlertaVencimiento.IsEnabled = llevaVencimiento;

            if (!llevaVencimiento)
            {
                DpFechaVencimiento.SelectedDate = null;
                TxtAlertaVencimiento.Text = "0";
            }

            UpdateVencimientoStatus();
        }

        private void FechaVencimiento_Changed(object sender, SelectionChangedEventArgs e)
        {
            UpdateVencimientoStatus();
        }

        private void AlertaVencimiento_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateVencimientoStatus();
        }

        private void UpdateVencimientoStatus()
        {
            if (BorderEstadoVencimiento == null || TxtEstadoVencimiento == null)
            {
                return;
            }

            if (ChkLlevaVencimiento.IsChecked != true)
            {
                BorderEstadoVencimiento.Background = new SolidColorBrush(Color.FromRgb(248, 249, 250));
                BorderEstadoVencimiento.BorderBrush = new SolidColorBrush(Color.FromRgb(204, 204, 204));
                TxtEstadoVencimiento.Text = "Active el control de vencimiento para recibir alertas sobre productos próximos a vencer.";
                TxtEstadoVencimiento.Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102));
                return;
            }

            if (DpFechaVencimiento.SelectedDate == null)
            {
                BorderEstadoVencimiento.Background = new SolidColorBrush(Color.FromRgb(255, 235, 230));
                BorderEstadoVencimiento.BorderBrush = new SolidColorBrush(Color.FromRgb(230, 126, 34));
                TxtEstadoVencimiento.Text = "⚠️ Debe establecer una fecha de vencimiento.";
                TxtEstadoVencimiento.Foreground = new SolidColorBrush(Color.FromRgb(230, 126, 34));
                return;
            }

            var fechaVencimiento = DpFechaVencimiento.SelectedDate.Value;
            var diasParaVencer = (fechaVencimiento - DateTime.Now).Days;

            int.TryParse(TxtAlertaVencimiento.Text, out var diasAlerta);

            if (diasParaVencer < 0)
            {
                BorderEstadoVencimiento.Background = new SolidColorBrush(Color.FromRgb(255, 220, 220));
                BorderEstadoVencimiento.BorderBrush = new SolidColorBrush(Color.FromRgb(231, 76, 60));
                TxtEstadoVencimiento.Text = $"🚫 PRODUCTO VENCIDO: Venció hace {Math.Abs(diasParaVencer)} días.";
                TxtEstadoVencimiento.Foreground = new SolidColorBrush(Color.FromRgb(231, 76, 60));
            }
            else if (diasParaVencer == 0)
            {
                BorderEstadoVencimiento.Background = new SolidColorBrush(Color.FromRgb(255, 220, 220));
                BorderEstadoVencimiento.BorderBrush = new SolidColorBrush(Color.FromRgb(231, 76, 60));
                TxtEstadoVencimiento.Text = "🚫 ALERTA CRÍTICA: El producto vence HOY.";
                TxtEstadoVencimiento.Foreground = new SolidColorBrush(Color.FromRgb(231, 76, 60));
            }
            else if (diasAlerta > 0 && diasParaVencer <= diasAlerta)
            {
                BorderEstadoVencimiento.Background = new SolidColorBrush(Color.FromRgb(255, 235, 230));
                BorderEstadoVencimiento.BorderBrush = new SolidColorBrush(Color.FromRgb(230, 126, 34));
                TxtEstadoVencimiento.Text = $"⚠️ ALERTA: El producto vence en {diasParaVencer} días ({fechaVencimiento:dd/MM/yyyy}).";
                TxtEstadoVencimiento.Foreground = new SolidColorBrush(Color.FromRgb(230, 126, 34));
            }
            else
            {
                BorderEstadoVencimiento.Background = new SolidColorBrush(Color.FromRgb(230, 255, 237));
                BorderEstadoVencimiento.BorderBrush = new SolidColorBrush(Color.FromRgb(39, 174, 96));
                TxtEstadoVencimiento.Text = $"✓ Producto vigente: Vence en {diasParaVencer} días ({fechaVencimiento:dd/MM/yyyy}).";
                TxtEstadoVencimiento.Foreground = new SolidColorBrush(Color.FromRgb(39, 174, 96));
            }
        }

        private void CargarReceta()
        {
            _receta.Clear();

            if (_product != null && !_isNewProduct)
            {
                using var ctx = new AppDbContext();
                var recetas = ctx.ProductRecipes
                    .Where(pr => pr.ProductId == _product.Id)
                    .Join(ctx.RawMaterials,
                        pr => pr.RawMaterialId,
                        rm => rm.Id,
                        (pr, rm) => new ProductRecipeViewModel
                        {
                            Id = pr.Id,
                            RawMaterialId = rm.Id,
                            RawMaterialCode = rm.Code,
                            RawMaterialName = rm.Name,
                            Quantity = pr.Quantity,
                            Unit = rm.Unit,
                            AvailableQuantity = rm.AvailableQuantity
                        })
                    .ToList();

                foreach (var receta in recetas)
                {
                    _receta.Add(receta);
                }
            }

            DgReceta.ItemsSource = _receta;
            ActualizarContadorIngredientes();
        }

        private void ActualizarContadorIngredientes()
        {
            TxtContadorIngredientes.Text = $"{_receta.Count} / 10 ingredientes";
        }

        private void ChkUsaReceta_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool usaReceta = ChkUsaReceta.IsChecked == true;

            PanelReceta.IsEnabled = usaReceta;
            MensajeSinReceta.Visibility = usaReceta ? Visibility.Collapsed : Visibility.Visible;

            if (!usaReceta)
            {
                // No limpiar la receta, solo deshabilitar
            }
        }

        private void BtnAgregarIngrediente_Click(object sender, RoutedEventArgs e)
        {
            if (_product == null || _isNewProduct)
            {
                MessageBox.Show("Debe guardar el producto antes de agregar ingredientes a la receta.",
                    "Información",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            if (_receta.Count >= 10)
            {
                MessageBox.Show("Ha alcanzado el límite máximo de 10 ingredientes por receta.",
                    "Límite alcanzado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var dialog = new RawMaterialSelectionDialog(_product.Id);
            if (dialog.ShowDialog() == true && dialog.SelectedRawMaterial != null)
            {
                // Verificar que no esté ya agregado
                if (_receta.Any(r => r.RawMaterialId == dialog.SelectedRawMaterial.RawMaterialId))
                {
                    MessageBox.Show("Este ingrediente ya está en la receta.",
                        "Ingrediente duplicado",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var nuevoIngrediente = new ProductRecipeViewModel
                {
                    RawMaterialId = dialog.SelectedRawMaterial.RawMaterialId,
                    RawMaterialCode = dialog.SelectedRawMaterial.RawMaterialCode,
                    RawMaterialName = dialog.SelectedRawMaterial.RawMaterialName,
                    Quantity = dialog.SelectedRawMaterial.Quantity,
                    Unit = dialog.SelectedRawMaterial.Unit,
                    AvailableQuantity = dialog.SelectedRawMaterial.AvailableQuantity
                };

                _receta.Add(nuevoIngrediente);
                ActualizarContadorIngredientes();
                DgReceta.Items.Refresh();
            }
        }

        private void BtnQuitarIngrediente_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var ingrediente = button?.Tag as ProductRecipeViewModel;

            if (ingrediente != null)
            {
                var result = MessageBox.Show(
                    $"¿Está seguro que desea quitar '{ingrediente.RawMaterialName}' de la receta?",
                    "Confirmar",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _receta.Remove(ingrediente);
                    ActualizarContadorIngredientes();
                    DgReceta.Items.Refresh();
                }
            }
        }

        private void BtnGuardarReceta_Click(object sender, RoutedEventArgs e)
        {
            if (_product == null || _isNewProduct)
            {
                MessageBox.Show("Debe guardar el producto antes de guardar la receta.",
                    "Información",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            try
            {
                using var ctx = new AppDbContext();

                // Eliminar receta existente
                var existentes = ctx.ProductRecipes.Where(pr => pr.ProductId == _product.Id).ToList();
                ctx.ProductRecipes.RemoveRange(existentes);

                // Agregar la nueva receta
                foreach (var ingrediente in _receta)
                {
                    ctx.ProductRecipes.Add(new ProductRecipe
                    {
                        ProductId = _product.Id,
                        RawMaterialId = ingrediente.RawMaterialId,
                        Quantity = ingrediente.Quantity,
                        CreatedAt = DateTime.Now
                    });
                }

                ctx.SaveChanges();

                MessageBox.Show("✓ Receta guardada correctamente.", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar receta:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAyudaReceta_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Receta del Producto:\n\n" +
                "• Usa Receta: Active esta opción si el producto se prepara con ingredientes/insumos.\n\n" +
                "• Ingredientes: Solo se pueden agregar materias primas marcadas como 'Ingrediente'.\n\n" +
                "• Cantidad: Especifique cuánto de cada ingrediente se necesita para preparar 1 unidad del producto.\n\n" +
                "• Stock Disponible: Muestra la cantidad actual disponible de cada ingrediente.\n\n" +
                "Al vender un producto con receta:\n" +
                "- Se descuenta automáticamente la cantidad de cada ingrediente del stock\n" +
                "- Se valida que haya suficiente stock antes de la venta\n\n" +
                "Ejemplo: Hamburguesa necesita:\n" +
                "- Carne picada: 0.150 kg\n" +
                "- Pan: 1 unidad\n" +
                "- Lechuga: 0.020 kg",
                "Ayuda - Receta del Producto",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void BtnAyudaStock_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Control de Stock:\n\n" +
                "• Lleva Stock: Active esta opción si el producto tiene inventario físico.\n\n" +
                "• Stock Actual: Cantidad actual de unidades disponibles (solo lectura).\n\n" +
                "• Stock Mínimo: Cuando el stock baje de este valor, recibirás una alerta.\n\n" +
                "• Stock Máximo: Cuando el stock supere este valor, recibirás una alerta.\n\n" +
                "El indicador de estado te mostrará el estado actual del stock con códigos de colores:\n" +
                "- Verde: Stock en rango normal\n" +
                "- Naranja: Stock por debajo del mínimo\n" +
                "- Azul: Stock por encima del máximo",
                "Ayuda - Control de Stock",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void BtnAyudaVencimiento_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Control de Vencimiento:\n\n" +
                "• Lleva Vencimiento: Active esta opción si el producto tiene fecha de vencimiento.\n\n" +
                "• Fecha de Vencimiento: Fecha en la que el producto vence y no debe venderse.\n\n" +
                "• Alerta Vencimiento: Número de días antes del vencimiento en los que desea recibir una alerta.\n\n" +
                "El indicador de estado mostrará:\n" +
                "- Rojo: Producto vencido o vence hoy\n" +
                "- Naranja: Producto próximo a vencer (dentro del período de alerta)\n" +
                "- Verde: Producto vigente\n\n" +
                "Es importante revisar regularmente los productos próximos a vencer para evitar pérdidas.",
                "Ayuda - Control de Vencimiento",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}