using FastFoodApp.Core.Data;
using FastFoodApp.Core.Models;
using System.Linq;
using System.Windows;

namespace FastFoodApp.Client
{
    public partial class PriceListDialog : Window
    {
        private int? _priceListId;
        private bool _isProtected;

        public PriceListDialog(int? priceListId = null)
        {
            InitializeComponent();
            _priceListId = priceListId;

            if (_priceListId.HasValue)
            {
                CargarDatos();
            }
            else
            {
                TxtTitulo.Text = "NUEVA LISTA DE PRECIOS";
            }

            TxtNombre.Focus();
        }

        private void CargarDatos()
        {
            using var ctx = new AppDbContext();
            var priceList = ctx.PriceLists.Find(_priceListId.Value);

            if (priceList != null)
            {
                TxtTitulo.Text = "EDITAR LISTA DE PRECIOS";
                TxtNombre.Text = priceList.Name;
                TxtDescripcion.Text = priceList.Description;
                ChkActiva.IsChecked = priceList.IsActive;
                _isProtected = priceList.IsProtected;

                if (_isProtected)
                {
                    TxtNombre.IsEnabled = false;
                    BorderProtegida.Visibility = Visibility.Visible;
                }
            }
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // Validaciones
            if (string.IsNullOrWhiteSpace(TxtNombre.Text))
            {
                MessageBox.Show("Debe ingresar un nombre para la lista de precios.",
                    "Validación",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                TxtNombre.Focus();
                return;
            }

            try
            {
                using var ctx = new AppDbContext();

                if (_priceListId.HasValue)
                {
                    // Editar existente
                    var priceList = ctx.PriceLists.Find(_priceListId.Value);
                    if (priceList != null)
                    {
                        // Solo permitir cambiar nombre si no es protegida
                        if (!_isProtected)
                        {
                            priceList.Name = TxtNombre.Text.Trim();
                        }

                        priceList.Description = TxtDescripcion.Text.Trim();
                        priceList.IsActive = ChkActiva.IsChecked == true;
                    }
                }
                else
                {
                    // Crear nueva
                    var nuevaLista = new PriceList
                    {
                        Name = TxtNombre.Text.Trim(),
                        Description = TxtDescripcion.Text.Trim(),
                        IsActive = ChkActiva.IsChecked == true,
                        IsProtected = false
                    };

                    ctx.PriceLists.Add(nuevaLista);
                }

                ctx.SaveChanges();

                MessageBox.Show(
                    "Lista de precios guardada correctamente.",
                    "Éxito",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al guardar la lista de precios:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}