using KioscoApp.Core.Data;
using KioscoApp.Core.Models;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace KioscoApp.Client
{
    public partial class ProductsWindow : Window
    {
        private bool _mostrarDiscontinuados = false;

        public ProductsWindow()
        {
            InitializeComponent();

            // Actualizar visibilidad de botones según selección
            GridProducts.SelectionChanged += GridProducts_SelectionChanged;

            // Cargar productos al final, cuando todo esté listo
            Loaded += (s, e) => CargarProductos();
        }

        private void CargarProductos()
        {
            if (GridProducts == null) return; // Protección

            using var ctx = new AppDbContext();
            var query = ctx.Products.AsQueryable();

            // Filtrar por texto de búsqueda - CON VALIDACIÓN
            if (TxtBuscar != null && !string.IsNullOrWhiteSpace(TxtBuscar.Text))
            {
                var textoBusqueda = TxtBuscar.Text.ToLower().Trim();
                query = query.Where(p =>
                    p.Code.ToLower().Contains(textoBusqueda) ||
                    p.Name.ToLower().Contains(textoBusqueda));
            }

            // Filtrar por categoría - CON VALIDACIÓN
            if (CmbCategoria != null && CmbCategoria.SelectedItem != null)
            {
                var categoriaSeleccionada = (CmbCategoria.SelectedItem as ComboBoxItem)?.Content.ToString();
                if (!string.IsNullOrEmpty(categoriaSeleccionada) && categoriaSeleccionada != "Todas")
                {
                    query = query.Where(p => p.Category == categoriaSeleccionada);
                }
            }

            // Filtrar por estado (activo/discontinuado)
            if (!_mostrarDiscontinuados)
            {
                query = query.Where(p => p.IsActive);
            }

            GridProducts.ItemsSource = query.OrderBy(p => p.Name).ToList();
        }

        private void GridProducts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Actualizar visibilidad de botones según el estado del producto seleccionado
            if (GridProducts.SelectedItem is Product prod)
            {
                BtnDiscontinuar.Visibility = prod.IsActive ? Visibility.Visible : Visibility.Collapsed;
                BtnReactivar.Visibility = prod.IsActive ? Visibility.Collapsed : Visibility.Visible;

                // Lo mismo para el menú contextual
                if (GridProducts.ContextMenu != null)
                {
                    foreach (var item in GridProducts.ContextMenu.Items)
                    {
                        if (item is MenuItem menuItem)
                        {
                            if (menuItem.Name == "MenuDiscontinuar")
                                menuItem.Visibility = prod.IsActive ? Visibility.Visible : Visibility.Collapsed;
                            else if (menuItem.Name == "MenuReactivar")
                                menuItem.Visibility = prod.IsActive ? Visibility.Collapsed : Visibility.Visible;
                        }
                    }
                }
            }
            else
            {
                BtnDiscontinuar.Visibility = Visibility.Visible;
                BtnReactivar.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnNuevo_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new ProductDialog();
            if (dlg.ShowDialog() == true)
            {
                CargarProductos();
            }
        }

        private void BtnModificar_Click(object sender, RoutedEventArgs e)
        {
            if (GridProducts.SelectedItem is Product prod)
            {
                var dlg = new ProductDialog(prod);
                if (dlg.ShowDialog() == true)
                {
                    CargarProductos();
                }
            }
            else
            {
                MessageBox.Show("Seleccione un producto para modificar.", "Información",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnDiscontinuar_Click(object sender, RoutedEventArgs e)
        {
            if (GridProducts.SelectedItem is Product prod)
            {
                var result = MessageBox.Show(
                    $"¿Está seguro que desea discontinuar el producto '{prod.Name}'?\n\n" +
                    "El producto no aparecerá en las ventas pero se mantendrá en el historial.",
                    "Confirmar Discontinuar",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    using var ctx = new AppDbContext();
                    var p = ctx.Products.FirstOrDefault(x => x.Id == prod.Id);
                    if (p != null)
                    {
                        p.IsActive = false;
                        ctx.SaveChanges();
                        CargarProductos();
                        MessageBox.Show("Producto discontinuado correctamente.", "Éxito",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            else
            {
                MessageBox.Show("Seleccione un producto para discontinuar.", "Información",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnReactivar_Click(object sender, RoutedEventArgs e)
        {
            if (GridProducts.SelectedItem is Product prod)
            {
                var result = MessageBox.Show(
                    $"¿Desea reactivar el producto '{prod.Name}'?",
                    "Confirmar Reactivación",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    using var ctx = new AppDbContext();
                    var p = ctx.Products.FirstOrDefault(x => x.Id == prod.Id);
                    if (p != null)
                    {
                        p.IsActive = true;
                        ctx.SaveChanges();
                        CargarProductos();
                        MessageBox.Show("Producto reactivado correctamente.", "Éxito",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            else
            {
                MessageBox.Show("Seleccione un producto para reactivar.", "Información",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void MenuVerDetalles_Click(object sender, RoutedEventArgs e)
        {
            if (GridProducts.SelectedItem is Product prod)
            {
                // Abrir en modo solo lectura
                var dlg = new ProductDialog(prod, isReadOnly: true);
                dlg.ShowDialog();
            }
        }

        private void MenuEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (GridProducts.SelectedItem is Product prod)
            {
                var result = MessageBox.Show(
                    $"⚠️ ADVERTENCIA ⚠️\n\n" +
                    $"¿Está seguro que desea ELIMINAR permanentemente el producto '{prod.Name}'?\n\n" +
                    "Esta acción NO se puede deshacer.\n" +
                    "Si solo desea que no aparezca en ventas, use 'Discontinuar' en su lugar.",
                    "Confirmar Eliminación",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    using var ctx = new AppDbContext();
                    var p = ctx.Products.FirstOrDefault(x => x.Id == prod.Id);
                    if (p != null)
                    {
                        ctx.Products.Remove(p);
                        ctx.SaveChanges();
                        CargarProductos();
                        MessageBox.Show("Producto eliminado permanentemente.", "Éxito",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }

        private void TxtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            CargarProductos();
        }

        private void CmbCategoria_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CargarProductos();
        }

        private void ChkMostrarDiscontinuados_Changed(object sender, RoutedEventArgs e)
        {
            _mostrarDiscontinuados = ChkMostrarDiscontinuados.IsChecked == true;
            CargarProductos();
        }

        private void GridProducts_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Doble clic para modificar
            if (GridProducts.SelectedItem is Product)
            {
                BtnModificar_Click(sender, e);
            }
        }
    }

    // Converter para mostrar el estado como texto
    public class BoolToEstadoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isActive)
            {
                return isActive ? "Activo" : "Discontinuado";
            }
            return "Desconocido";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}