using FastFoodApp.Core.Data;
using FastFoodApp.Core.Models;
using System;
using System.Linq;
using System.Windows;

namespace FastFoodApp.Client
{
    public partial class SupplierSelectionDialog : Window
    {
        public class ProveedorViewModel
        {
            public int SupplierId { get; set; }
            public string NombreProveedor { get; set; } = string.Empty;
            public decimal Costo { get; set; }
        }

        private int _productId;
        public ProveedorViewModel? SelectedSupplier { get; private set; }

        public SupplierSelectionDialog(int productId)
        {
            InitializeComponent();
            _productId = productId;
            CargarProveedores();
        }

        private void CargarProveedores(string filtro = "")
        {
            using var ctx = new AppDbContext();

            // Obtener proveedores activos que NO estén ya asociados a este producto
            var proveedoresAsociados = ctx.ProductSuppliers
                .Where(ps => ps.ProductId == _productId)
                .Select(ps => ps.SupplierId)
                .ToList();

            var query = ctx.Suppliers
                .Where(s => s.IsActive && !proveedoresAsociados.Contains(s.Id));

            if (!string.IsNullOrWhiteSpace(filtro))
            {
                query = query.Where(s => s.Name.Contains(filtro));
            }

            LstProveedores.ItemsSource = query.ToList();
        }

        private void TxtBuscar_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            CargarProveedores(TxtBuscar.Text);
        }

        private void BtnAceptar_Click(object sender, RoutedEventArgs e)
        {
            if (LstProveedores.SelectedItem == null)
            {
                MessageBox.Show("Debe seleccionar un proveedor.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var supplier = (Supplier)LstProveedores.SelectedItem;
            SelectedSupplier = new ProveedorViewModel
            {
                SupplierId = supplier.Id,
                NombreProveedor = supplier.Name,
                Costo = 0 // El costo se establece después en la grilla editable
            };

            DialogResult = true;
            Close();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnCrearProveedor_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SupplierDialog();
            if (dialog.ShowDialog() == true)
            {
                // Recargar la lista de proveedores
                CargarProveedores(TxtBuscar.Text);

                MessageBox.Show("Proveedor creado correctamente.", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}