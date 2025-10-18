using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace FastFoodApp.Client
{
    public partial class PaymentWindow : Window
    {
        public decimal TotalVenta { get; set; }
        public decimal TotalPagado { get; private set; }
        public decimal RestaPagar => TotalVenta - TotalPagado;
        public bool PagoConfirmado { get; private set; }

        // Propiedades individuales de los métodos de pago
        public decimal Efectivo => ParseDecimal(TxtEfectivo.Text);
        public decimal TarjetaCredito => ParseDecimal(TxtTarjetaCredito.Text);
        public decimal CuentaCorriente => ParseDecimal(TxtCuentaCorriente.Text);
        public decimal Cheque => ParseDecimal(TxtCheque.Text);
        public decimal Tickets => ParseDecimal(TxtTickets.Text);

        public PaymentWindow(decimal totalVenta)
        {
            InitializeComponent();
            TotalVenta = totalVenta;

            InicializarVentana();
            AsociarEventos();
        }

        private void InicializarVentana()
        {
            LblTotalAPagar.Text = TotalVenta.ToString("C", CultureInfo.CurrentCulture);
            LblTotalPagado.Text = "$0.00";
            LblRestaPagar.Text = TotalVenta.ToString("C", CultureInfo.CurrentCulture);
        }

        private void AsociarEventos()
        {
            TxtEfectivo.TextChanged += Inputs_TextChanged;
            TxtTarjetaCredito.TextChanged += Inputs_TextChanged;
            TxtCuentaCorriente.TextChanged += Inputs_TextChanged;
            TxtCheque.TextChanged += Inputs_TextChanged;
            TxtTickets.TextChanged += Inputs_TextChanged;

            BtnConfirmar.Click += BtnConfirmar_Click;
            BtnCancelar.Click += BtnCancelar_Click;
        }

        private void Inputs_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalcularTotales();
        }

        private void CalcularTotales()
        {
            TotalPagado = Efectivo + TarjetaCredito + CuentaCorriente + Cheque + Tickets;

            // Actualizar labels
            LblTotalPagado.Text = TotalPagado.ToString("C", CultureInfo.CurrentCulture);
            LblRestaPagar.Text = (TotalVenta - TotalPagado).ToString("C", CultureInfo.CurrentCulture);
        }

        private decimal ParseDecimal(string input)
        {
            if (decimal.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
                return value;
            if (decimal.TryParse(input, NumberStyles.Any, CultureInfo.CurrentCulture, out value))
                return value;
            return 0;
        }

        private void BtnConfirmar_Click(object sender, RoutedEventArgs e)
        {
            if (TotalPagado < TotalVenta)
            {
                MessageBox.Show("El monto pagado es insuficiente", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            PagoConfirmado = true;
            DialogResult = true;
            Close();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            PagoConfirmado = false;
            DialogResult = false;
            Close();
        }

        // Método privado de ayuda para sumar montos en efectivo
        private void AgregarEfectivo(decimal monto)
        {
            decimal actual = ParseDecimal(TxtEfectivo.Text);
            actual += monto;
            TxtEfectivo.Text = actual.ToString("F2", CultureInfo.InvariantCulture);
        }

        // Métodos públicos para enlazar a botones en XAML
        public void BtnAgregar5_Click(object sender, RoutedEventArgs e) => AgregarEfectivo(5);
        public void BtnAgregar10_Click(object sender, RoutedEventArgs e) => AgregarEfectivo(10);
        public void BtnAgregar20_Click(object sender, RoutedEventArgs e) => AgregarEfectivo(20);
        public void BtnAgregar50_Click(object sender, RoutedEventArgs e) => AgregarEfectivo(50);
        public void BtnAgregar100_Click(object sender, RoutedEventArgs e) => AgregarEfectivo(100);
    }
}
