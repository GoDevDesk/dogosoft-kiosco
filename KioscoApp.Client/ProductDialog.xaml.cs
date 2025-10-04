using KioscoApp.Core.Data;
using KioscoApp.Core.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace KioscoApp.Client
{
    public partial class ProductDialog : Window
    {
        private readonly Product? _product;
        private readonly bool _isReadOnly;
        private readonly bool _isNewProduct;

        public ProductDialog(Product? product = null, bool isReadOnly = false)
        {
            InitializeComponent();

            _isReadOnly = isReadOnly;
            _isNewProduct = product == null;

            if (product != null)
            {
                using var ctx = new AppDbContext();
                _product = ctx.Products.FirstOrDefault(p => p.Id == product.Id);

                if (_product != null)
                {
                    LoadProductData();
                    Title = _isReadOnly ? "Ver artículo" : "Editar artículo";
                }
            }
            else
            {
                _product = new Product();
                Title = "Nuevo artículo";

                TxtPrecioVenta.Text = "0.00";
                TxtPrecioCosto.Text = "0.00";
                TxtObservaciones.Text = "";
            }

            if (_isReadOnly)
            {
                SetControlsReadOnly();
                BtnGuardar.Visibility = Visibility.Collapsed;
                BtnCancelar.Content = "Cerrar";
            }

            TxtCode.TextChanged += OnTextChanged;
            TxtDescription.TextChanged += OnTextChanged;
            TxtObservaciones.TextChanged += OnTextChanged;
            TxtPrecioVenta.TextChanged += OnTextChanged;
        }

        private void LoadProductData()
        {
            if (_product == null) return;

            TxtCode.Text = _product.Code ?? "";
            TxtDescription.Text = _product.Name ?? "";
            TxtObservaciones.Text = _product.Observations ?? "";
            TxtPrecioVenta.Text = _product.Price.ToString("F2");

            if (!string.IsNullOrEmpty(_product.Category))
            {
                CmbRubro.Text = _product.Category;
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
                    // Si hay error al cargar la imagen, mostrar placeholder
                    ImgProducto.Visibility = Visibility.Collapsed;
                    PlaceholderImagen.Visibility = Visibility.Visible;
                }
            }

            UpdatePreview();
        }

        private void SetControlsReadOnly()
        {
            TxtCode.IsReadOnly = true;
            TxtDescription.IsReadOnly = true;
            TxtObservaciones.IsReadOnly = true;
            TxtPrecioVenta.IsReadOnly = true;
            TxtPrecioCosto.IsReadOnly = true;
            CmbRubro.IsEnabled = false;
            CmbUnidadVenta.IsEnabled = false;

            var grayBrush = System.Windows.Media.Brushes.WhiteSmoke;
            TxtCode.Background = grayBrush;
            TxtDescription.Background = grayBrush;
            TxtObservaciones.Background = grayBrush;
            TxtPrecioVenta.Background = grayBrush;
            TxtPrecioCosto.Background = grayBrush;
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            var nombrePreview = !string.IsNullOrWhiteSpace(TxtObservaciones.Text)
                ? TxtObservaciones.Text
                : TxtDescription.Text;

            TxtPreviewName.Text = nombrePreview.ToUpper();
            TxtPreviewCode.Text = TxtCode.Text;

            if (decimal.TryParse(TxtPrecioVenta.Text, out var precioVenta))
            {
                TxtPreviewPrice.Text = $"$ {precioVenta:F2}";
                TxtPreviewCost.Text = $"${precioVenta:F2}/UN";
            }
            else
            {
                TxtPreviewPrice.Text = "$ 0,00";
                TxtPreviewCost.Text = "$0,00/UN";
            }

            TxtPreviewDate.Text = DateTime.Now.ToString("dd/MM/yy");
            TxtPreviewQuantity.Text = "";
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // Validaciones
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

            try
            {
                using var ctx = new AppDbContext();

                if (_isNewProduct)
                {
                    // Verificar que no exista el código
                    if (ctx.Products.Any(p => p.Code == TxtCode.Text.Trim()))
                    {
                        MessageBox.Show("Ya existe un producto con ese código.", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Crear un NUEVO producto directamente en el contexto
                    var nuevoProducto = new Product
                    {
                        Code = TxtCode.Text.Trim(),
                        Name = TxtDescription.Text.Trim(),
                        Observations = TxtObservaciones.Text.Trim(),
                        Price = decimal.TryParse(TxtPrecioVenta.Text, out var pv) ? pv : 0,
                        Category = string.IsNullOrWhiteSpace(CmbRubro.Text) ? null : CmbRubro.Text,
                        Stock = 0,
                        IsActive = true,
                        ImagePath = _product?.ImagePath
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
                        existing.Price = decimal.TryParse(TxtPrecioVenta.Text, out var pv) ? pv : 0;
                        existing.Category = string.IsNullOrWhiteSpace(CmbRubro.Text) ? null : CmbRubro.Text;
                        existing.ImagePath = _product.ImagePath;
                    }
                }

                ctx.SaveChanges();

                MessageBox.Show("Producto guardado correctamente.", "Éxito",
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
                    // Guardar la ruta de la imagen anterior antes de reemplazarla
                    string? rutaImagenAnterior = _product?.ImagePath;

                    // Guardar la nueva ruta de la imagen
                    if (_product != null)
                    {
                        _product.ImagePath = openFileDialog.FileName;
                    }

                    // Mostrar la imagen
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(openFileDialog.FileName);
                    bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    ImgProducto.Source = bitmap;
                    PlaceholderImagen.Visibility = Visibility.Collapsed;
                    ImgProducto.Visibility = Visibility.Visible;

                    // Eliminar el archivo físico anterior si existía
                    if (!string.IsNullOrEmpty(rutaImagenAnterior) &&
                        System.IO.File.Exists(rutaImagenAnterior) &&
                        rutaImagenAnterior != openFileDialog.FileName) // Evitar borrar si es la misma
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
            // Verificar si hay una imagen cargada
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
                    // Guardar la ruta antes de limpiarla por si necesitamos eliminar el archivo
                    string? rutaImagenAnterior = _product.ImagePath;

                    _product.ImagePath = null;

                    // Limpiar la UI
                    ImgProducto.Source = null;
                    ImgProducto.Visibility = Visibility.Collapsed;
                    PlaceholderImagen.Visibility = Visibility.Visible;

                    // Si NO es un producto nuevo, guardar el cambio en BD
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

                    // Eliminar el archivo físico si existe
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
    }
}