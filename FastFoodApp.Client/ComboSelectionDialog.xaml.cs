using FastFoodApp.Core.Data;
using FastFoodApp.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;

namespace FastFoodApp.Client
{
    public partial class ComboSelectionDialog : Window
    {
        private readonly int _comboId;
        private List<ComboItemViewModel> _comboItems = new();

        public Dictionary<int, int> ProductosSeleccionados { get; private set; } = new();
        public decimal PrecioCombo { get; private set; }

        public ComboSelectionDialog(int comboId)
        {
            InitializeComponent();
            _comboId = comboId;
            CargarCombo();
        }

        private void CargarCombo()
        {
            using var ctx = new AppDbContext();

            var combo = ctx.Combos
                .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefault(c => c.Id == _comboId);

            if (combo == null)
            {
                MessageBox.Show("Combo no encontrado");
                Close();
                return;
            }

            TxtNombreCombo.Text = combo.Name;
            TxtDescripcionCombo.Text = combo.Description;
            TxtPrecioCombo.Text = combo.Price.ToString("C");
            PrecioCombo = combo.Price;

            // Cargar items del combo
            foreach (var item in combo.Items)
            {
                var viewModel = new ComboItemViewModel
                {
                    ComboItemId = item.Id,
                    GroupName = item.SubstitutionGroup ?? item.Product.Name,
                    Icon = GetProductIcon(item.Product.Name),
                    Quantity = item.Quantity,
                    QuantityText = item.Quantity > 1 ? $"(x{item.Quantity})" : "",
                    DefaultProductId = item.ProductId,
                    DefaultProductName = item.Product.Name,
                    AllowsSubstitution = item.AllowsSubstitution
                };

                if (item.AllowsSubstitution)
                {
                    // Agregar producto por defecto
                    viewModel.Options.Add(new ComboOptionViewModel
                    {
                        ProductId = item.ProductId,
                        ProductName = item.Product.Name,
                        IsSelected = true,
                        GroupName = viewModel.GroupName
                    });

                    // Agregar opciones alternativas
                    var alternativas = ctx.ComboSubstitutionOptions
                        .Include(cso => cso.AlternativeProduct)
                        .Where(cso => cso.ComboItemId == item.Id)
                        .ToList();

                    foreach (var alt in alternativas)
                    {
                        viewModel.Options.Add(new ComboOptionViewModel
                        {
                            ProductId = alt.AlternativeProductId,
                            ProductName = alt.AlternativeProduct.Name,
                            IsSelected = false,
                            GroupName = viewModel.GroupName
                        });
                    }

                    viewModel.HasOptions = Visibility.Visible;
                    viewModel.IsFixed = Visibility.Collapsed;
                }
                else
                {
                    viewModel.HasOptions = Visibility.Collapsed;
                    viewModel.IsFixed = Visibility.Visible;
                }

                _comboItems.Add(viewModel);

                // Registrar selección inicial
                ProductosSeleccionados[item.Id] = item.ProductId;
            }

            ComboItemsControl.ItemsSource = _comboItems;
        }

        private string GetProductIcon(string productName)
        {
            var name = productName.ToLower();
            if (name.Contains("hamburgues")) return "🍔";
            if (name.Contains("pizza")) return "🍕";
            if (name.Contains("papa")) return "🍟";
            if (name.Contains("bebida") || name.Contains("coca") || name.Contains("agua")) return "🥤";
            if (name.Contains("postre")) return "🍰";
            return "🍴";
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag is int productId)
            {
                // Encontrar el ComboItemViewModel correspondiente
                foreach (var item in _comboItems)
                {
                    var option = item.Options.FirstOrDefault(o => o.ProductId == productId);
                    if (option != null)
                    {
                        ProductosSeleccionados[item.ComboItemId] = productId;
                        break;
                    }
                }
            }
        }

        private void BtnAceptar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    #region ViewModels

    public class ComboItemViewModel
    {
        public int ComboItemId { get; set; }
        public string GroupName { get; set; } = "";
        public string Icon { get; set; } = "🍴";
        public int Quantity { get; set; }
        public string QuantityText { get; set; } = "";
        public int DefaultProductId { get; set; }
        public string DefaultProductName { get; set; } = "";
        public bool AllowsSubstitution { get; set; }
        public Visibility HasOptions { get; set; } = Visibility.Collapsed;
        public Visibility IsFixed { get; set; } = Visibility.Visible;
        public List<ComboOptionViewModel> Options { get; set; } = new();
    }

    public class ComboOptionViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public bool IsSelected { get; set; }
        public string GroupName { get; set; } = "";
    }

    #endregion
}