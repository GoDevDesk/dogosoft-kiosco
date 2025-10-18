using FastFoodApp.Core.Data;
using FastFoodApp.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace FastFoodApp.Client
{
    public partial class ComboEditorDialog : Window
    {
        private int? _comboId;
        private ObservableCollection<ComboProductViewModel> _productos = new();

        public ComboEditorDialog(int? comboId = null)
        {
            InitializeComponent();
            _comboId = comboId;
            CargarCategorias();

            if (_comboId.HasValue)
            {
                TxtTitulo.Text = "✏️ Editar Combo";
                CargarCombo(_comboId.Value);
            }
            else
            {
                TxtTitulo.Text = "🎁 Crear Nuevo Combo";
            }

            ProductosItemsControl.ItemsSource = _productos;
            ActualizarVisibilidadProductosVacio();
        }

        private void CargarCategorias()
        {
            using var ctx = new AppDbContext();
            var categorias = ctx.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToList();

            CmbCategoria.ItemsSource = categorias;

            // Seleccionar "Combos" por defecto
            var combosCategoria = categorias.FirstOrDefault(c => c.Name == "Combos");
            if (combosCategoria != null)
            {
                CmbCategoria.SelectedValue = combosCategoria.Id;
            }
            else if (categorias.Count > 0)
            {
                CmbCategoria.SelectedIndex = 0;
            }
        }

        private void CargarCombo(int comboId)
        {
            using var ctx = new AppDbContext();

            var combo = ctx.Combos
                .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefault(c => c.Id == comboId);

            if (combo == null)
            {
                MessageBox.Show("Combo no encontrado", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            TxtNombre.Text = combo.Name;
            TxtDescripcion.Text = combo.Description;
            TxtPrecio.Text = combo.Price.ToString("F2");

            if (combo.CategoryId.HasValue)
            {
                CmbCategoria.SelectedValue = combo.CategoryId.Value;
            }

            // Cargar productos del combo
            foreach (var item in combo.Items)
            {
                var productoVm = new ComboProductViewModel
                {
                    ComboItemId = item.Id,
                    ProductId = item.ProductId,
                    ProductName = item.Product.Name,
                    Icon = GetProductIcon(item.Product.Name),
                    Quantity = item.Quantity,
                    QuantityText = $"Cantidad: {item.Quantity}",
                    AllowsSubstitution = item.AllowsSubstitution,
                    SubstitutionGroup = item.SubstitutionGroup ?? item.Product.Name,
                    SubstitutionVisibility = item.AllowsSubstitution ? Visibility.Visible : Visibility.Collapsed,
                    Alternatives = new ObservableCollection<AlternativeProductViewModel>()
                };

                // Cargar alternativas si permite sustitución
                if (item.AllowsSubstitution)
                {
                    var alternativas = ctx.ComboSubstitutionOptions
                        .Include(cso => cso.AlternativeProduct)
                        .Where(cso => cso.ComboItemId == item.Id)
                        .ToList();

                    foreach (var alt in alternativas)
                    {
                        productoVm.Alternatives.Add(new AlternativeProductViewModel
                        {
                            SubstitutionOptionId = alt.Id,
                            AlternativeProductId = alt.AlternativeProductId,
                            AlternativeProductName = alt.AlternativeProduct.Name
                        });
                    }
                }

                _productos.Add(productoVm);
            }
        }

        private string GetProductIcon(string productName)
        {
            var name = productName.ToLower();
            if (name.Contains("hamburgues")) return "🍔";
            if (name.Contains("pizza")) return "🍕";
            if (name.Contains("papa")) return "🍟";
            if (name.Contains("bebida") || name.Contains("coca") || name.Contains("agua")) return "🥤";
            if (name.Contains("postre")) return "🍰";
            if (name.Contains("ensalada")) return "🥗";
            if (name.Contains("sandwich")) return "🥪";
            return "🍴";
        }

        private void BtnAgregarProducto_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ProductSelectorDialog();
            dialog.Owner = this;

            if (dialog.ShowDialog() == true && dialog.ProductoSeleccionado != null)
            {
                var producto = dialog.ProductoSeleccionado;

                // Verificar que no esté ya agregado
                if (_productos.Any(p => p.ProductId == producto.Id))
                {
                    MessageBox.Show("Este producto ya está en el combo", "Producto duplicado",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var productoVm = new ComboProductViewModel
                {
                    ProductId = producto.Id,
                    ProductName = producto.Name,
                    Icon = GetProductIcon(producto.Name),
                    Quantity = 1,
                    QuantityText = "Cantidad: 1",
                    AllowsSubstitution = false,
                    SubstitutionGroup = producto.Name,
                    SubstitutionVisibility = Visibility.Collapsed,
                    Alternatives = new ObservableCollection<AlternativeProductViewModel>()
                };

                _productos.Add(productoVm);
                ActualizarVisibilidadProductosVacio();
            }
        }

        private void BtnEliminarProducto_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ComboProductViewModel producto)
            {
                var result = MessageBox.Show(
                    $"¿Eliminar '{producto.ProductName}' del combo?",
                    "Confirmar eliminación",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _productos.Remove(producto);
                    ActualizarVisibilidadProductosVacio();
                }
            }
        }

        private void ChkPermiteSubstitucion_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox chk)
            {
                // Encontrar el producto correspondiente
                var parent = FindParent<Border>(chk);
                if (parent?.DataContext is ComboProductViewModel producto)
                {
                    producto.SubstitutionVisibility = chk.IsChecked == true
                        ? Visibility.Visible
                        : Visibility.Collapsed;

                    // Refrescar el ItemsControl
                    ProductosItemsControl.Items.Refresh();
                }
            }
        }

        private void BtnAgregarAlternativa_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ComboProductViewModel producto)
            {
                var dialog = new ProductSelectorDialog();
                dialog.Owner = this;

                if (dialog.ShowDialog() == true && dialog.ProductoSeleccionado != null)
                {
                    var alternativa = dialog.ProductoSeleccionado;

                    // Verificar que no sea el mismo producto
                    if (alternativa.Id == producto.ProductId)
                    {
                        MessageBox.Show("No puedes agregar el mismo producto como alternativa",
                            "Producto duplicado", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Verificar que no esté ya en las alternativas
                    if (producto.Alternatives.Any(a => a.AlternativeProductId == alternativa.Id))
                    {
                        MessageBox.Show("Esta alternativa ya está agregada",
                            "Alternativa duplicada", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    producto.Alternatives.Add(new AlternativeProductViewModel
                    {
                        AlternativeProductId = alternativa.Id,
                        AlternativeProductName = alternativa.Name
                    });
                }
            }
        }

        private void BtnEliminarAlternativa_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is AlternativeProductViewModel alternativa)
            {
                // Buscar el producto padre
                foreach (var producto in _productos)
                {
                    if (producto.Alternatives.Contains(alternativa))
                    {
                        producto.Alternatives.Remove(alternativa);
                        break;
                    }
                }
            }
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // Validaciones
            if (string.IsNullOrWhiteSpace(TxtNombre.Text))
            {
                MessageBox.Show("Ingrese el nombre del combo", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtNombre.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtPrecio.Text) || !decimal.TryParse(TxtPrecio.Text, out decimal precio) || precio <= 0)
            {
                MessageBox.Show("Ingrese un precio válido", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtPrecio.Focus();
                return;
            }

            if (_productos.Count == 0)
            {
                MessageBox.Show("Agregue al menos un producto al combo", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CmbCategoria.SelectedValue == null)
            {
                MessageBox.Show("Seleccione una categoría", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var ctx = new AppDbContext();

                Combo combo;
                if (_comboId.HasValue)
                {
                    // Editar combo existente
                    combo = ctx.Combos
                        .Include(c => c.Items)
                        .FirstOrDefault(c => c.Id == _comboId.Value);

                    if (combo == null)
                    {
                        MessageBox.Show("Combo no encontrado", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Eliminar items anteriores
                    var itemsAnteriores = ctx.ComboItems
                        .Include(ci => ci.Product)
                        .Where(ci => ci.ComboId == combo.Id)
                        .ToList();

                    foreach (var item in itemsAnteriores)
                    {
                        // Eliminar opciones de sustitución
                        var opciones = ctx.ComboSubstitutionOptions
                            .Where(cso => cso.ComboItemId == item.Id);
                        ctx.ComboSubstitutionOptions.RemoveRange(opciones);
                    }

                    ctx.ComboItems.RemoveRange(itemsAnteriores);

                    // Actualizar datos básicos
                    combo.Name = TxtNombre.Text.Trim();
                    combo.Description = TxtDescripcion.Text.Trim();
                    combo.Price = precio;
                    combo.CategoryId = (int)CmbCategoria.SelectedValue;
                }
                else
                {
                    // Crear nuevo combo
                    combo = new Combo
                    {
                        Name = TxtNombre.Text.Trim(),
                        Description = TxtDescripcion.Text.Trim(),
                        Price = precio,
                        CategoryId = (int)CmbCategoria.SelectedValue,
                        IsActive = true
                    };

                    ctx.Combos.Add(combo);
                }

                ctx.SaveChanges();

                // Agregar items del combo
                foreach (var productoVm in _productos)
                {
                    var comboItem = new ComboItem
                    {
                        ComboId = combo.Id,
                        ProductId = productoVm.ProductId,
                        Quantity = productoVm.Quantity,
                        AllowsSubstitution = productoVm.AllowsSubstitution,
                        SubstitutionGroup = productoVm.AllowsSubstitution ? productoVm.SubstitutionGroup : null
                    };

                    ctx.ComboItems.Add(comboItem);
                    ctx.SaveChanges();

                    // Agregar alternativas si permite sustitución
                    if (productoVm.AllowsSubstitution)
                    {
                        foreach (var alternativa in productoVm.Alternatives)
                        {
                            ctx.ComboSubstitutionOptions.Add(new ComboSubstitutionOption
                            {
                                ComboItemId = comboItem.Id,
                                AlternativeProductId = alternativa.AlternativeProductId
                            });
                        }
                    }
                }

                ctx.SaveChanges();

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar el combo: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ActualizarVisibilidadProductosVacio()
        {
            ProductosVacioPanel.Visibility = _productos.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        // Helper para encontrar padre en el árbol visual
        private T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = System.Windows.Media.VisualTreeHelper.GetParent(child);

            if (parentObject == null) return null;

            if (parentObject is T parent)
                return parent;
            else
                return FindParent<T>(parentObject);
        }
    }

    #region ViewModels

    public class ComboProductViewModel
    {
        public int ComboItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string Icon { get; set; } = "🍴";
        public int Quantity { get; set; } = 1;
        public string QuantityText { get; set; } = "";
        public bool AllowsSubstitution { get; set; }
        public string SubstitutionGroup { get; set; } = "";
        public Visibility SubstitutionVisibility { get; set; } = Visibility.Collapsed;
        public ObservableCollection<AlternativeProductViewModel> Alternatives { get; set; } = new();
    }

    public class AlternativeProductViewModel
    {
        public int SubstitutionOptionId { get; set; }
        public int AlternativeProductId { get; set; }
        public string AlternativeProductName { get; set; } = "";
    }

    #endregion
}