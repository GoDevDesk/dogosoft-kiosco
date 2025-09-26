using System;
using System.Linq;
using System.Windows;
using KioscoApp.Core.Data;
using KioscoApp.Core.Services;

namespace KioscoApp.Client
{
    public partial class MainWindow : Window
    {
        private readonly LicenseService _licenseSvc = new LicenseService();

        public MainWindow()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            var lic = _licenseSvc.GetLicense();
            TxtLicense.Text = $"Licencia: {lic.Type}";
            TxtExpires.Text = lic.Expiry.HasValue ? $" - Expira: {lic.Expiry.Value.ToShortDateString()}" : string.Empty;

            using var ctx = new AppDbContext();
            GridProducts.ItemsSource = ctx.Products.OrderBy(p => p.Name).ToList();
        }

        private void BtnIngresarClave_Click(object sender, RoutedEventArgs e)
        {
            var input = new SimpleInputDialog("Ingrese clave de licencia:").ShowDialogAndReturn();
            if (!string.IsNullOrWhiteSpace(input))
            {
                _licenseSvc.ActivateLicense(input, "Paga", null);
                MessageBox.Show("Licencia activada. Reinicie la aplicación.");
                LoadData();
            }
        }

        private void BtnNuevaVenta_Click(object sender, RoutedEventArgs e)
        {
            var posWindow = new PosWindow();
            posWindow.ShowDialog(); // Se abre la ventana POS como modal
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnHistorialVentas_Click(object sender, RoutedEventArgs e)
        {
            var window = new SalesHistoryWindow();
            window.ShowDialog(); // muestra la ventana modal
        }

        private void BtnProductos_Click(object sender, RoutedEventArgs e)
        {
            var window = new ProductsWindow();
            window.ShowDialog(); // muestra la ventana modal
        }


    }
}
