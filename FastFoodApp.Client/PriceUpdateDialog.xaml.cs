using FastFoodApp.Core.Data;
using FastFoodApp.Core.Models;
using System;
using System.Linq;
using System.Windows;

namespace FastFoodApp.Client
{
    public partial class PriceUpdateDialog : Window
    {
        public PriceUpdateDialog()
        {
            InitializeComponent();
            CargarDatos();
        }

        private void CargarDatos()
        {
            using var ctx = new AppDbContext();

            // Cargar listas de precios
            var listas = ctx.PriceLists.Where(pl => pl.IsActive).ToList();
            CmbLista.ItemsSource = listas;
            if (listas.Count > 0)
            {
                CmbLista.SelectedIndex = 0;
            }

            // Cargar rubros desde la tabla Categories
            var rubros = ctx.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToList();

            // Agregar rubros al combo
            foreach (var rubro in rubros)
            {
                var item = new System.Windows.Controls.ComboBoxItem
                {
                    Content = rubro.Name,
                    Tag = rubro.Id // Usar el ID en lugar del nombre
                };
                CmbRubro.Items.Add(item);
            }
        }

        private void BtnAplicar_Click(object sender, RoutedEventArgs e)
        {
            // Validaciones
            if (CmbLista.SelectedValue == null)
            {
                MessageBox.Show("Debe seleccionar una lista de precios.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(TxtVariacion.Text, out var porcentaje))
            {
                MessageBox.Show("Debe ingresar un porcentaje válido.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtVariacion.Focus();
                return;
            }

            if (porcentaje == 0)
            {
                MessageBox.Show("El porcentaje no puede ser cero.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtVariacion.Focus();
                return;
            }

            var resultado = MessageBox.Show(
                $"¿Está seguro que desea aplicar una variación del {porcentaje}% a los precios de costo seleccionados?\n\n" +
                "Esta acción modificará los precios de costo y recalculará los precios de venta según el porcentaje de utilidad configurado.",
                "Confirmar Actualización",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (resultado != MessageBoxResult.Yes)
                return;

            try
            {
                using var ctx = new AppDbContext();

                var listaId = (int)CmbLista.SelectedValue;
                var rubroTag = (CmbRubro.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag?.ToString();

                // Obtener registros de ProductPriceList según filtros
                var query = ctx.ProductPriceLists
                    .Where(ppl => ppl.PriceListId == listaId)
                    .AsQueryable();

                // Filtrar por rubro si no es "Todos"
                if (rubroTag != "ALL")
                {
                    if (int.TryParse(rubroTag, out var categoryId))
                    {
                        var productosDelRubro = ctx.Products
                            .Where(p => p.CategoryId == categoryId)
                            .Select(p => p.Id)
                            .ToList();

                        query = query.Where(ppl => productosDelRubro.Contains(ppl.ProductId));
                    }
                }

                var productosParaActualizar = query.ToList();

                if (productosParaActualizar.Count == 0)
                {
                    MessageBox.Show("No se encontraron productos con los filtros seleccionados.", "Información",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Aplicar el porcentaje al CostPrice y recalcular SalePrice
                int actualizados = 0;
                foreach (var productoPrecio in productosParaActualizar)
                {
                    // Calcular nuevo costo
                    var nuevoCosto = productoPrecio.CostPrice * (1 + (porcentaje / 100));
                    productoPrecio.CostPrice = Math.Round(nuevoCosto, 2);

                    // IMPORTANTE: Recalcular el precio de venta según el porcentaje de utilidad
                    // Fórmula: PrecioVenta = Costo + (Costo × % Utilidad / 100)
                    if (productoPrecio.CostPrice > 0 && productoPrecio.ProfitPercentage >= 0)
                    {
                        var nuevoPrecioVenta = productoPrecio.CostPrice +
                                              (productoPrecio.CostPrice * productoPrecio.ProfitPercentage / 100);
                        productoPrecio.SalePrice = Math.Round(nuevoPrecioVenta, 2);
                    }

                    productoPrecio.LastUpdate = DateTime.Now;
                    actualizados++;
                }

                ctx.SaveChanges();

                MessageBox.Show(
                    $"Actualización completada exitosamente.\n\n" +
                    $"Productos actualizados: {actualizados}\n" +
                    $"Variación aplicada: {porcentaje}%\n\n" +
                    $"Se actualizaron los precios de costo y se recalcularon los precios de venta.",
                    "Éxito",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al actualizar precios:\n{ex.Message}", "Error",
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