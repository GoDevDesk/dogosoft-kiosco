using KioscoApp.Core.Data;
using KioscoApp.Core.Models;
using System.Linq;
using System.Windows;

namespace KioscoApp.Client
{
    public partial class ProductsWindow : Window
    {
        public ProductsWindow()
        {
            InitializeComponent();
            CargarProductos();
        }

        private void CargarProductos()
        {
            using var ctx = new AppDbContext();
            GridProducts.ItemsSource = ctx.Products.OrderBy(p => p.Name).ToList();
        }

        private void BtnNuevo_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new ProductDialog(); // el diálogo guarda el producto
            if (dlg.ShowDialog() == true)
            {
                // Solo recargamos la grilla
                CargarProductos();
            }
        }

        private void BtnModificar_Click(object sender, RoutedEventArgs e)
        {
            if (GridProducts.SelectedItem is Product prod)
            {
                var dlg = new ProductDialog(prod); // el diálogo edita directamente en DB
                if (dlg.ShowDialog() == true)
                {
                    // Recargamos la grilla después de la edición
                    CargarProductos();
                }
            }
            else
            {
                MessageBox.Show("Seleccione un producto para modificar.");
            }
        }

        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (GridProducts.SelectedItem is Product prod)
            {
                if (MessageBox.Show($"¿Eliminar el producto {prod.Name}?", "Confirmar", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    using var ctx = new AppDbContext();
                    var p = ctx.Products.FirstOrDefault(x => x.Id == prod.Id);
                    if (p != null)
                    {
                        ctx.Products.Remove(p);
                        ctx.SaveChanges();
                        CargarProductos();
                    }
                }
            }
            else
            {
                MessageBox.Show("Seleccione un producto para eliminar.");
            }
        }
    }
}
