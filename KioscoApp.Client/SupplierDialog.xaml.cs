using KioscoApp.Core.Data;
using KioscoApp.Core.Models;
using System;
using System.Linq;
using System.Windows;

namespace KioscoApp.Client
{
    public partial class SupplierDialog : Window
    {
        private readonly Supplier? _supplier;
        private readonly bool _isNewSupplier;

        public SupplierDialog(Supplier? supplier = null)
        {
            InitializeComponent();

            _isNewSupplier = supplier == null;

            if (supplier != null)
            {
                using var ctx = new AppDbContext();
                _supplier = ctx.Suppliers.FirstOrDefault(s => s.Id == supplier.Id);

                if (_supplier != null)
                {
                    LoadSupplierData();
                    Title = "Editar Proveedor";
                }
            }
            else
            {
                _supplier = new Supplier();
                Title = "Nuevo Proveedor";
            }
        }

        private void LoadSupplierData()
        {
            if (_supplier == null) return;

            TxtNombre.Text = _supplier.Name;
            TxtContacto.Text = _supplier.ContactPerson ?? "";
            TxtTelefono.Text = _supplier.Phone ?? "";
            TxtEmail.Text = _supplier.Email ?? "";
            TxtDireccion.Text = _supplier.Address ?? "";
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // Validación
            if (string.IsNullOrWhiteSpace(TxtNombre.Text))
            {
                MessageBox.Show("El nombre del proveedor es obligatorio.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtNombre.Focus();
                return;
            }

            try
            {
                using var ctx = new AppDbContext();

                if (_isNewSupplier)
                {
                    // Verificar que no exista el nombre
                    if (ctx.Suppliers.Any(s => s.Name == TxtNombre.Text.Trim()))
                    {
                        MessageBox.Show("Ya existe un proveedor con ese nombre.", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var newSupplier = new Supplier
                    {
                        Name = TxtNombre.Text.Trim(),
                        ContactPerson = string.IsNullOrWhiteSpace(TxtContacto.Text) ? null : TxtContacto.Text.Trim(),
                        Phone = string.IsNullOrWhiteSpace(TxtTelefono.Text) ? null : TxtTelefono.Text.Trim(),
                        Email = string.IsNullOrWhiteSpace(TxtEmail.Text) ? null : TxtEmail.Text.Trim(),
                        Address = string.IsNullOrWhiteSpace(TxtDireccion.Text) ? null : TxtDireccion.Text.Trim(),
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };

                    ctx.Suppliers.Add(newSupplier);
                }
                else
                {
                    if (_supplier == null) return;

                    var existing = ctx.Suppliers.FirstOrDefault(s => s.Id == _supplier.Id);
                    if (existing != null)
                    {
                        existing.Name = TxtNombre.Text.Trim();
                        existing.ContactPerson = string.IsNullOrWhiteSpace(TxtContacto.Text) ? null : TxtContacto.Text.Trim();
                        existing.Phone = string.IsNullOrWhiteSpace(TxtTelefono.Text) ? null : TxtTelefono.Text.Trim();
                        existing.Email = string.IsNullOrWhiteSpace(TxtEmail.Text) ? null : TxtEmail.Text.Trim();
                        existing.Address = string.IsNullOrWhiteSpace(TxtDireccion.Text) ? null : TxtDireccion.Text.Trim();
                    }
                }

                ctx.SaveChanges();

                MessageBox.Show("Proveedor guardado correctamente.", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                MessageBox.Show($"Error al guardar el proveedor:\n{innerMessage}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}