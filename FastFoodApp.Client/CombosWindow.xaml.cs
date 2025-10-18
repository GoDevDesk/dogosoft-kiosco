using FastFoodApp.Core.Data;
using FastFoodApp.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;

namespace FastFoodApp.Client
{
    public partial class CombosWindow : Window
    {
        public CombosWindow()
        {
            InitializeComponent();
            CargarCombos();
        }

        private void CargarCombos()
        {
            using var ctx = new AppDbContext();

            var combos = ctx.Combos
                .Include(c => c.Items)
                .Include(c => c.Category)
                .OrderBy(c => c.Name)
                .ToList();

            var combosViewModel = combos.Select(c => new ComboListViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Price = c.Price,
                ItemCount = c.Items.Count,
                IsActive = c.IsActive,
                StatusText = c.IsActive ? "✓ Activo" : "✗ Inactivo"
            }).ToList();

            GridCombos.ItemsSource = combosViewModel;
        }

        private void BtnNuevoCombo_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ComboEditorDialog();
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                CargarCombos();
                MessageBox.Show("✓ Combo creado correctamente", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnEditar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int comboId)
            {
                var dialog = new ComboEditorDialog(comboId);
                dialog.Owner = this;

                if (dialog.ShowDialog() == true)
                {
                    CargarCombos();
                    MessageBox.Show("✓ Combo actualizado correctamente", "Éxito",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int comboId)
            {
                var result = MessageBox.Show(
                    "¿Está seguro que desea eliminar este combo?",
                    "Confirmar eliminación",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    using var ctx = new AppDbContext();
                    var combo = ctx.Combos.Find(comboId);

                    if (combo != null)
                    {
                        combo.IsActive = false; // Soft delete
                        ctx.SaveChanges();
                        CargarCombos();

                        MessageBox.Show("✓ Combo desactivado correctamente", "Éxito",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class ComboListViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Price { get; set; }
        public int ItemCount { get; set; }
        public bool IsActive { get; set; }
        public string StatusText { get; set; } = "";
    }
}