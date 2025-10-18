using FastFoodApp.Core.Data;
using FastFoodApp.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Input;

namespace FastFoodApp.Client
{
    public partial class ProductSelectorDialog : Window
    {
        public Product? ProductoSeleccionado { get; private set; }

        public ProductSelectorDialog()
        {
            InitializeComponent();
            CargarProductos();
        }

        private void CargarProductos(string? filtro = null)
        {
            using var ctx = new AppDbContext();

            IQueryable<Product> query = ctx.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive);

            if (!string.IsNullOrWhiteSpace(filtro))
            {
                filtro = filtro.ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(filtro) ||
                    p.Code.ToLower().Contains(filtro));
            }

            var productos = query
                .OrderBy(p => p.Name)
                .Select(p => new ProductoListViewModel
                {
                    Product = p,
                    Code = p.Code,
                    Name = p.Name,
                    CategoryName = p.Category != null ? p.Category.Name : "Sin categoría",
                    Stock = p.Stock ?? 0  // ✅ CORREGIDO: convertir nullable a int
                })
                .ToList();

            GridProductos.ItemsSource = productos;
        }
        private void TxtBuscar_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            CargarProductos(TxtBuscar.Text);
        }

        private void GridProductos_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SeleccionarProducto();
        }

        private void BtnSeleccionar_Click(object sender, RoutedEventArgs e)
        {
            SeleccionarProducto();
        }

        private void SeleccionarProducto()
        {
            if (GridProductos.SelectedItem is ProductoListViewModel productoVm)
            {
                ProductoSeleccionado = productoVm.Product;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Seleccione un producto", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class ProductoListViewModel
    {
        public Product Product { get; set; } = null!;
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public int Stock { get; set; }
    }
}