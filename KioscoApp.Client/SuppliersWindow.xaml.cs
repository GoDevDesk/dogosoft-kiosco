using KioscoApp.Core.Data;
using KioscoApp.Core.Models;
using System;
using System.Linq;
using System.Windows;

namespace KioscoApp.Client
{
    public partial class SuppliersWindow : Window
    {
        public SuppliersWindow()
        {
            InitializeComponent();
            CargarProveedores();
        }

        private void CargarProveedores(string filtro = "")
        {
            using var ctx = new AppDbContext();

            var query = ctx.Suppliers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filtro))
            {
                query = query.Where(s => s.Name.Contains(filtro) ||
                                        (s.ContactPerson != null && s.ContactPerson.Contains(filtro)) ||
                                        (s.Phone != null && s.Phone.Contains(filtro)));
            }

            DgProveedores.ItemsSource = query.OrderBy(s => s.Name).ToList();
        }

        private void TxtBuscar_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            CargarProveedores(TxtBuscar.Text);
        }

        private void BtnNuevo_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SupplierDialog();
            if (dialog.ShowDialog() == true)
            {
                CargarProveedores(TxtBuscar.Text);
                MessageBox.Show("Proveedor creado correctamente.", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnVer_Click(object sender, RoutedEventArgs e)
        {
            if (DgProveedores.SelectedItem is Supplier supplier)
            {
                var dialog = new SupplierDialog(supplier);
                dialog.ShowDialog();
            }
            else
            {
                MessageBox.Show("Seleccione un proveedor.", "Información",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnEditar_Click(object sender, RoutedEventArgs e)
        {
            if (DgProveedores.SelectedItem is Supplier supplier)
            {
                var dialog = new SupplierDialog(supplier);
                if (dialog.ShowDialog() == true)
                {
                    CargarProveedores(TxtBuscar.Text);
                    MessageBox.Show("Proveedor actualizado correctamente.", "Éxito",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Seleccione un proveedor para editar.", "Información",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}