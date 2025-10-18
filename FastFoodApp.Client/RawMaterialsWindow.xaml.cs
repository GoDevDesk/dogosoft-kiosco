using FastFoodApp.Core.Data;
using FastFoodApp.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace FastFoodApp.Client
{
    public class RawMaterialViewModel
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsIngredient { get; set; }
        public string TipoTexto => IsIngredient ? "🥬 Ingrediente" : "📦 Insumo";
        public string Unit { get; set; } = string.Empty;
        public decimal AvailableQuantity { get; set; }
        public decimal? UnitCost { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
    }

    public partial class RawMaterialsWindow : Window
    {
        private List<RawMaterialViewModel> _todosLosInsumos;
        private List<RawMaterialViewModel> _insumosFiltrados;

        public RawMaterialsWindow()
        {
            InitializeComponent();
            _todosLosInsumos = new List<RawMaterialViewModel>();
            _insumosFiltrados = new List<RawMaterialViewModel>();
            CargarInsumos();
        }

        private void CargarInsumos()
        {
            using var ctx = new AppDbContext();

            _todosLosInsumos = ctx.RawMaterials
                .Include(rm => rm.Category)
                .Include(rm => rm.Supplier)
                .Where(rm => rm.IsActive)
                .Select(rm => new RawMaterialViewModel
                {
                    Id = rm.Id,
                    Code = rm.Code,
                    Name = rm.Name,
                    IsIngredient = rm.IsIngredient,
                    Unit = rm.Unit,
                    AvailableQuantity = rm.AvailableQuantity,
                    UnitCost = rm.UnitCost,
                    CategoryName = rm.Category != null ? rm.Category.Name : "-",
                    SupplierName = rm.Supplier != null ? rm.Supplier.Name : "-"
                })
                .OrderBy(rm => rm.Name)
                .ToList();

            AplicarFiltros();
        }

        private void AplicarFiltros()
        {
            var busqueda = TxtBuscar.Text.ToLower().Trim();
            var soloIngredientes = ChkSoloIngredientes.IsChecked == true;

            _insumosFiltrados = _todosLosInsumos
                .Where(i =>
                    (string.IsNullOrWhiteSpace(busqueda) ||
                     i.Code.ToLower().Contains(busqueda) ||
                     i.Name.ToLower().Contains(busqueda)) &&
                    (!soloIngredientes || i.IsIngredient))
                .ToList();

            DgInsumos.ItemsSource = null;
            DgInsumos.ItemsSource = _insumosFiltrados;
        }

        private void TxtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            AplicarFiltros();
        }

        private void FiltrarInsumos(object sender, RoutedEventArgs e)
        {
            AplicarFiltros();
        }

        private void BtnNuevo_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new RawMaterialDialog();
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                CargarInsumos();
                MessageBox.Show("✓ Insumo creado correctamente.", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnVer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is RawMaterialViewModel insumo)
            {
                using var ctx = new AppDbContext();
                var rawMaterial = ctx.RawMaterials.FirstOrDefault(rm => rm.Id == insumo.Id);

                if (rawMaterial != null)
                {
                    var dialog = new RawMaterialDialog(rawMaterial, isReadOnly: true);
                    dialog.Owner = this;
                    dialog.ShowDialog();
                }
            }
        }

        private void BtnEditar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is RawMaterialViewModel insumo)
            {
                using var ctx = new AppDbContext();
                var rawMaterial = ctx.RawMaterials.FirstOrDefault(rm => rm.Id == insumo.Id);

                if (rawMaterial != null)
                {
                    var dialog = new RawMaterialDialog(rawMaterial, isReadOnly: false);
                    dialog.Owner = this;

                    if (dialog.ShowDialog() == true)
                    {
                        CargarInsumos();
                        MessageBox.Show("✓ Insumo actualizado correctamente.", "Éxito",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }

        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is RawMaterialViewModel insumo)
            {
                // Verificar si el insumo está siendo usado en recetas
                using var ctx = new AppDbContext();
                var enUso = ctx.ProductRecipes.Any(pr => pr.RawMaterialId == insumo.Id);

                if (enUso)
                {
                    MessageBox.Show(
                        $"No se puede eliminar '{insumo.Name}' porque está siendo utilizado en una o más recetas.\n\n" +
                        "Primero debe eliminar las recetas que lo usan.",
                        "Insumo en uso",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    $"¿Está seguro que desea eliminar el insumo '{insumo.Name}'?\n\n" +
                    "Esta acción marcará el insumo como inactivo.",
                    "Confirmar eliminación",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var rawMaterial = ctx.RawMaterials.FirstOrDefault(rm => rm.Id == insumo.Id);
                        if (rawMaterial != null)
                        {
                            rawMaterial.IsActive = false;
                            rawMaterial.UpdatedAt = DateTime.Now;
                            ctx.SaveChanges();

                            CargarInsumos();
                            MessageBox.Show("✓ Insumo eliminado correctamente.", "Éxito",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al eliminar el insumo:\n{ex.Message}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}