using FastFoodApp.Core.Data;
using FastFoodApp.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace FastFoodApp.Client
{
    public class RawMaterialSelectionViewModel
    {
        public int RawMaterialId { get; set; }
        public string RawMaterialCode { get; set; } = string.Empty;
        public string RawMaterialName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal AvailableQuantity { get; set; }
    }

    public class RawMaterialListItem
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal AvailableQuantity { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }

    public partial class RawMaterialSelectionDialog : Window
    {
        private readonly int _productId;
        private List<RawMaterialListItem> _ingredientes;
        private List<RawMaterialListItem> _ingredientesFiltrados;

        public RawMaterialSelectionViewModel? SelectedRawMaterial { get; private set; }

        public RawMaterialSelectionDialog(int productId)
        {
            InitializeComponent();
            _productId = productId;
            _ingredientes = new List<RawMaterialListItem>();
            _ingredientesFiltrados = new List<RawMaterialListItem>();

            CargarIngredientes();
        }

        private void CargarIngredientes()
        {
            using var ctx = new AppDbContext();

            // Obtener IDs de ingredientes ya usados en esta receta
            var ingredientesYaUsados = ctx.ProductRecipes
                .Where(pr => pr.ProductId == _productId)
                .Select(pr => pr.RawMaterialId)
                .ToHashSet();

            // Cargar solo materias primas que son ingredientes (IsIngredient = true)
            // y que NO están ya en la receta
            _ingredientes = ctx.RawMaterials
                .Where(rm => rm.IsIngredient && rm.IsActive && !ingredientesYaUsados.Contains(rm.Id))
                .Select(rm => new RawMaterialListItem
                {
                    Id = rm.Id,
                    Code = rm.Code,
                    Name = rm.Name,
                    Unit = rm.Unit,
                    AvailableQuantity = rm.AvailableQuantity,
                    CategoryName = rm.Category != null ? rm.Category.Name : "Sin categoría"
                })
                .OrderBy(rm => rm.Name)
                .ToList();

            _ingredientesFiltrados = _ingredientes;
            DgIngredientes.ItemsSource = _ingredientesFiltrados;

            if (_ingredientes.Count == 0)
            {
                MessageBox.Show(
                    "No hay ingredientes disponibles para agregar.\n\n" +
                    "Posibles causas:\n" +
                    "• No hay materias primas marcadas como 'Ingrediente'\n" +
                    "• Todos los ingredientes ya están en la receta\n" +
                    "• No hay materias primas activas",
                    "Sin ingredientes",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                Close();
            }
        }

        private void TxtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            var busqueda = TxtBuscar.Text.ToLower().Trim();

            if (string.IsNullOrWhiteSpace(busqueda))
            {
                _ingredientesFiltrados = _ingredientes;
            }
            else
            {
                _ingredientesFiltrados = _ingredientes
                    .Where(i => i.Code.ToLower().Contains(busqueda) ||
                               i.Name.ToLower().Contains(busqueda))
                    .ToList();
            }

            DgIngredientes.ItemsSource = null;
            DgIngredientes.ItemsSource = _ingredientesFiltrados;
        }

        private void DgIngredientes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgIngredientes.SelectedItem is RawMaterialListItem selected)
            {
                TxtUnidad.Text = selected.Unit;
                TxtCantidad.Focus();
                TxtCantidad.SelectAll();
            }
        }

        private void BtnAceptar_Click(object sender, RoutedEventArgs e)
        {
            if (DgIngredientes.SelectedItem is not RawMaterialListItem selected)
            {
                MessageBox.Show("Debe seleccionar un ingrediente.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(TxtCantidad.Text, out var cantidad) || cantidad <= 0)
            {
                MessageBox.Show("Debe especificar una cantidad válida mayor a 0.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtCantidad.Focus();
                TxtCantidad.SelectAll();
                return;
            }

            // Validar que no exceda el stock disponible (advertencia, no bloqueo)
            if (cantidad > selected.AvailableQuantity)
            {
                var result = MessageBox.Show(
                    $"La cantidad especificada ({cantidad:N2} {selected.Unit}) supera el stock disponible ({selected.AvailableQuantity:N2} {selected.Unit}).\n\n" +
                    "¿Desea continuar de todas formas?",
                    "Stock insuficiente",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                {
                    TxtCantidad.Focus();
                    TxtCantidad.SelectAll();
                    return;
                }
            }

            SelectedRawMaterial = new RawMaterialSelectionViewModel
            {
                RawMaterialId = selected.Id,
                RawMaterialCode = selected.Code,
                RawMaterialName = selected.Name,
                Quantity = cantidad,
                Unit = selected.Unit,
                AvailableQuantity = selected.AvailableQuantity
            };

            DialogResult = true;
            Close();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}