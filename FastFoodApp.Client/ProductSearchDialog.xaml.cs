using FastFoodApp.Core.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FastFoodApp.Client
{
    public class ProductSearchItem
    {
        public int ProductId { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public int Cantidad { get; set; } = 1;
        public decimal Subtotal => Cantidad * Price;
    }

    public partial class ProductSearchDialog : Window
    {
        private ObservableCollection<ProductSearchItem> _productosSeleccionados;
        private readonly int _listaPrecionId;

        public List<ProductSearchItem> ProductosSeleccionados => _productosSeleccionados.ToList();

        public ProductSearchDialog(int listaPrecionId)
        {
            InitializeComponent();
            _listaPrecionId = listaPrecionId;
            _productosSeleccionados = new ObservableCollection<ProductSearchItem>();

            DgSeleccionados.ItemsSource = _productosSeleccionados;
            _productosSeleccionados.CollectionChanged += (s, e) => ActualizarTotal();

            TxtBuscar.Focus();
        }

        private void BtnAyuda_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "BÚSQUEDA AVANZADA DE PRODUCTOS:\n\n" +
                "1️⃣ BUSCAR PRODUCTOS:\n" +
                "   • Escriba al menos 2 caracteres en el campo de búsqueda\n" +
                "   • Puede buscar por código, nombre o categoría\n" +
                "   • Los resultados aparecerán automáticamente\n\n" +
                "2️⃣ AGREGAR PRODUCTOS:\n" +
                "   • Haga clic en el botón '➕ Agregar' de cada producto\n" +
                "   • O haga doble clic en el producto\n" +
                "   • O presione Enter con el producto seleccionado\n\n" +
                "3️⃣ MODIFICAR SELECCIÓN:\n" +
                "   • Si agrega un producto ya seleccionado, aumentará la cantidad\n" +
                "   • Use el botón '❌' para quitar productos de la selección\n\n" +
                "4️⃣ CONFIRMAR:\n" +
                "   • Presione F9 o haga clic en 'Agregar Todos a la Venta'\n" +
                "   • Los productos se agregarán al carrito de venta\n\n" +
                "⌨️ ATAJOS DE TECLADO:\n" +
                "   F1 - Esta ayuda\n" +
                "   Enter - Agregar producto seleccionado\n" +
                "   F9 - Confirmar y agregar todos\n" +
                "   Esc - Cancelar",
                "Ayuda - Búsqueda Avanzada",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void TxtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            var filtro = TxtBuscar.Text.Trim();

            // Requiere al menos 2 caracteres para buscar
            if (filtro.Length < 2)
            {
                BorderMensajeInicial.Visibility = Visibility.Visible;
                DgProductos.Visibility = Visibility.Collapsed;
                DgProductos.ItemsSource = null;
                return;
            }

            // Ocultar mensaje y mostrar grid
            BorderMensajeInicial.Visibility = Visibility.Collapsed;
            DgProductos.Visibility = Visibility.Visible;

            // Buscar en la base de datos
            BuscarProductos(filtro);
        }

        private void BuscarProductos(string filtro)
        {
            using var ctx = new AppDbContext();
            var filtroLower = filtro.ToLower();

            var productos = ctx.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive &&
                           p.TracksStock && p.Stock.HasValue && p.Stock.Value > 0 && // ✅ Validar stock correctamente
                           (p.Code.ToLower().Contains(filtroLower) ||
                            p.Name.ToLower().Contains(filtroLower) ||
                            (p.Category != null && p.Category.Name.ToLower().Contains(filtroLower))))
                .Select(p => new
                {
                    p.Id,
                    p.Code,
                    p.Name,
                    CategoryName = p.Category != null ? p.Category.Name : "Sin categoría",
                    Stock = p.Stock ?? 0  // ✅ Convertir nullable a int
                })
                .Take(100) // Limitar a 100 resultados para performance
                .ToList();

            var resultados = new List<ProductSearchItem>();

            foreach (var prod in productos)
            {
                // Obtener precio de la lista
                var precioLista = ctx.ProductPriceLists
                    .FirstOrDefault(ppl => ppl.ProductId == prod.Id && ppl.PriceListId == _listaPrecionId);

                if (precioLista != null)
                {
                    resultados.Add(new ProductSearchItem
                    {
                        ProductId = prod.Id,
                        Code = prod.Code,
                        Name = prod.Name,
                        CategoryName = prod.CategoryName,
                        Price = precioLista.SalePrice,
                        Stock = prod.Stock  // ✅ Ya es int porque usamos ?? 0 arriba
                    });
                }
            }

            DgProductos.ItemsSource = resultados;
        }

        private void BtnAgregarProducto_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ProductSearchItem producto)
            {
                AgregarProducto(producto);
            }
        }

        private void DgProductos_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DgProductos.SelectedItem is ProductSearchItem producto)
            {
                AgregarProducto(producto);
            }
        }

        private void AgregarProducto(ProductSearchItem producto)
        {
            // Verificar si ya está en los seleccionados
            var existente = _productosSeleccionados.FirstOrDefault(p => p.ProductId == producto.ProductId);

            if (existente != null)
            {
                if (existente.Cantidad + 1 > producto.Stock)
                {
                    MessageBox.Show($"No hay stock suficiente de '{producto.Name}'.\nStock disponible: {producto.Stock}",
                        "Stock insuficiente",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
                existente.Cantidad++;
                DgSeleccionados.Items.Refresh();
            }
            else
            {
                _productosSeleccionados.Add(new ProductSearchItem
                {
                    ProductId = producto.ProductId,
                    Code = producto.Code,
                    Name = producto.Name,
                    CategoryName = producto.CategoryName,
                    Price = producto.Price,
                    Stock = producto.Stock,
                    Cantidad = 1
                });
            }

            ActualizarTotal();
        }

        private void BtnQuitarProducto_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ProductSearchItem producto)
            {
                _productosSeleccionados.Remove(producto);
                ActualizarTotal();
            }
        }

        private void ActualizarTotal()
        {
            var total = _productosSeleccionados.Sum(p => p.Subtotal);
            TxtTotal.Text = $"Total: {total:C}";
        }

        private void BtnAgregarTodos_Click(object sender, RoutedEventArgs e)
        {
            if (_productosSeleccionados.Count == 0)
            {
                MessageBox.Show("No hay productos seleccionados para agregar.",
                    "Sin productos",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            if (_productosSeleccionados.Count > 0)
            {
                var result = MessageBox.Show(
                    "Hay productos seleccionados. ¿Está seguro que desea cancelar?",
                    "Confirmar",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                    return;
            }

            DialogResult = false;
            Close();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F1:
                    BtnAyuda_Click(sender, e);
                    e.Handled = true;
                    break;

                case Key.Enter:
                    if (DgProductos.SelectedItem is ProductSearchItem producto)
                    {
                        AgregarProducto(producto);
                        e.Handled = true;
                    }
                    break;

                case Key.F9:
                    if (_productosSeleccionados.Count > 0)
                    {
                        BtnAgregarTodos_Click(sender, e);
                        e.Handled = true;
                    }
                    break;
            }
        }
    }
}