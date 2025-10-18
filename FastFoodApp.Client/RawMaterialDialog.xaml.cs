using FastFoodApp.Core.Data;
using FastFoodApp.Core.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace FastFoodApp.Client
{
    public partial class RawMaterialDialog : Window
    {
        private readonly RawMaterial? _rawMaterial;
        private readonly bool _isReadOnly;
        private readonly bool _isNewRawMaterial;

        public RawMaterialDialog(RawMaterial? rawMaterial = null, bool isReadOnly = false)
        {
            InitializeComponent();

            _isReadOnly = isReadOnly;
            _isNewRawMaterial = rawMaterial == null;

            if (rawMaterial != null)
            {
                using var ctx = new AppDbContext();
                _rawMaterial = ctx.RawMaterials.FirstOrDefault(rm => rm.Id == rawMaterial.Id);

                if (_rawMaterial != null)
                {
                    LoadData();
                    TxtTitulo.Text = _isReadOnly ? "VER INSUMO" : "EDITAR INSUMO";
                    Title = _isReadOnly ? "Ver Insumo" : "Editar Insumo";
                }
            }
            else
            {
                _rawMaterial = new RawMaterial();
                TxtTitulo.Text = "NUEVO INSUMO";
                Title = "Nuevo Insumo";
            }

            if (_isReadOnly)
            {
                SetControlsReadOnly();
                BtnGuardar.Visibility = Visibility.Collapsed;
                BtnCancelar.Content = "✖ Cerrar";
            }

            CargarCategorias();
            CargarProveedores();
            ChkEsIngrediente.Checked += ChkEsIngrediente_CheckedChanged;
            ChkEsIngrediente.Unchecked += ChkEsIngrediente_CheckedChanged;

            // Inicializar visibilidad del panel de stock
            ActualizarVisibilidadPanelStock();
        }

        private void CargarCategorias()
        {
            using var ctx = new AppDbContext();
            var categorias = ctx.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToList();

            CmbCategory.ItemsSource = categorias;
        }

        private void CargarProveedores()
        {
            using var ctx = new AppDbContext();
            var proveedores = ctx.Suppliers
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToList();

            CmbSupplier.ItemsSource = proveedores;
        }

        private void LoadData()
        {
            if (_rawMaterial == null) return;

            TxtCode.Text = _rawMaterial.Code;
            TxtName.Text = _rawMaterial.Name;
            TxtDescription.Text = _rawMaterial.Description ?? "";
            ChkEsIngrediente.IsChecked = _rawMaterial.IsIngredient;
            TxtUnitCost.Text = _rawMaterial.UnitCost?.ToString() ?? "0";
            TxtAvailableQuantity.Text = _rawMaterial.AvailableQuantity.ToString("N2");
            TxtMinQuantity.Text = _rawMaterial.MinimumQuantity?.ToString() ?? "0";
            TxtMaxQuantity.Text = _rawMaterial.MaximumQuantity?.ToString() ?? "0";

            // Seleccionar unidad
            foreach (ComboBoxItem item in CmbUnit.Items)
            {
                if (item.Tag?.ToString() == _rawMaterial.Unit)
                {
                    CmbUnit.SelectedItem = item;
                    break;
                }
            }

            // Seleccionar categoría
            if (_rawMaterial.CategoryId.HasValue)
            {
                CmbCategory.SelectedValue = _rawMaterial.CategoryId.Value;
            }

            // Seleccionar proveedor
            if (_rawMaterial.SupplierId.HasValue)
            {
                CmbSupplier.SelectedValue = _rawMaterial.SupplierId.Value;
            }
        }

        private void SetControlsReadOnly()
        {
            TxtCode.IsReadOnly = true;
            TxtName.IsReadOnly = true;
            TxtDescription.IsReadOnly = true;
            TxtUnitCost.IsReadOnly = true;
            TxtAvailableQuantity.IsReadOnly = true;
            TxtMinQuantity.IsReadOnly = true;
            TxtMaxQuantity.IsReadOnly = true;
            ChkEsIngrediente.IsEnabled = false;
            CmbUnit.IsEnabled = false;
            CmbCategory.IsEnabled = false;
            CmbSupplier.IsEnabled = false;

            var grayBrush = System.Windows.Media.Brushes.WhiteSmoke;
            TxtCode.Background = grayBrush;
            TxtName.Background = grayBrush;
            TxtDescription.Background = grayBrush;
            TxtUnitCost.Background = grayBrush;
            TxtMinQuantity.Background = grayBrush;
            TxtMaxQuantity.Background = grayBrush;
        }

        private void ChkEsIngrediente_CheckedChanged(object sender, RoutedEventArgs e)
        {
            ActualizarVisibilidadPanelStock();
        }

        private void ActualizarVisibilidadPanelStock()
        {
            if (ChkEsIngrediente.IsChecked == true)
            {
                PanelStock.Visibility = Visibility.Visible;
            }
            else
            {
                PanelStock.Visibility = Visibility.Collapsed;
            }
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

            if (string.IsNullOrWhiteSpace(TxtName.Text))
            {
                MessageBox.Show("El nombre es obligatorio.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtName.Focus();
                return;
            }

            if (CmbUnit.SelectedItem == null)
            {
                MessageBox.Show("Debe seleccionar una unidad de medida.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CmbUnit.Focus();
                return;
            }

            try
            {
                using var ctx = new AppDbContext();

                if (_isNewRawMaterial)
                {
                    // Verificar código único
                    if (ctx.RawMaterials.Any(rm => rm.Code == TxtCode.Text.Trim()))
                    {
                        MessageBox.Show("Ya existe un insumo con ese código.", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var nuevoInsumo = new RawMaterial
                    {
                        Code = TxtCode.Text.Trim(),
                        Name = TxtName.Text.Trim(),
                        Description = TxtDescription.Text.Trim(),
                        IsIngredient = ChkEsIngrediente.IsChecked == true,
                        Unit = (CmbUnit.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "UN",
                        AvailableQuantity = 0, // Siempre inicia en 0
                        MinimumQuantity = ChkEsIngrediente.IsChecked == true && decimal.TryParse(TxtMinQuantity.Text, out var min) ? min : (decimal?)null,
                        MaximumQuantity = ChkEsIngrediente.IsChecked == true && decimal.TryParse(TxtMaxQuantity.Text, out var max) ? max : (decimal?)null,
                        UnitCost = decimal.TryParse(TxtUnitCost.Text, out var cost) ? cost : (decimal?)null,
                        CategoryId = CmbCategory.SelectedValue as int?,
                        SupplierId = CmbSupplier.SelectedValue as int?,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };

                    ctx.RawMaterials.Add(nuevoInsumo);
                }
                else
                {
                    if (_rawMaterial == null) return;

                    var existing = ctx.RawMaterials.FirstOrDefault(rm => rm.Id == _rawMaterial.Id);
                    if (existing != null)
                    {
                        // Verificar si está cambiando el tipo (IsIngredient)
                        if (existing.IsIngredient && ChkEsIngrediente.IsChecked == false)
                        {
                            // Verificar si está en uso en recetas
                            var enUsoEnRecetas = ctx.ProductRecipes.Any(pr => pr.RawMaterialId == existing.Id);
                            if (enUsoEnRecetas)
                            {
                                MessageBox.Show(
                                    "No puede cambiar este insumo a 'No Ingrediente' porque está siendo usado en recetas de productos.\n\n" +
                                    "Primero debe eliminar las recetas que lo usan.",
                                    "Insumo en uso",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                                return;
                            }
                        }

                        existing.Code = TxtCode.Text.Trim();
                        existing.Name = TxtName.Text.Trim();
                        existing.Description = TxtDescription.Text.Trim();
                        existing.IsIngredient = ChkEsIngrediente.IsChecked == true;
                        existing.Unit = (CmbUnit.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "UN";
                        existing.MinimumQuantity = ChkEsIngrediente.IsChecked == true && decimal.TryParse(TxtMinQuantity.Text, out var min) ? min : (decimal?)null;
                        existing.MaximumQuantity = ChkEsIngrediente.IsChecked == true && decimal.TryParse(TxtMaxQuantity.Text, out var max) ? max : (decimal?)null;
                        existing.UnitCost = decimal.TryParse(TxtUnitCost.Text, out var cost) ? cost : (decimal?)null;
                        existing.CategoryId = CmbCategory.SelectedValue as int?;
                        existing.SupplierId = CmbSupplier.SelectedValue as int?;
                        existing.UpdatedAt = DateTime.Now;
                    }
                }

                ctx.SaveChanges();

                MessageBox.Show("✓ Insumo guardado correctamente.", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                MessageBox.Show($"Error al guardar el insumo:\n{innerMessage}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            if (!_isReadOnly && !_isNewRawMaterial)
            {
                var result = MessageBox.Show("¿Desea descartar los cambios?", "Confirmar",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                    return;
            }

            DialogResult = false;
            Close();
        }
    }
}