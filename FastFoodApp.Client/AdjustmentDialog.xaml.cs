using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace FastFoodApp.Client
{
    public partial class AdjustmentDialog : Window
    {
        private decimal _subtotal;
        private bool _esDescuento;
        public decimal MontoAjuste { get; private set; }

        public AdjustmentDialog(decimal subtotal, bool esDescuento)
        {
            InitializeComponent();
            _subtotal = subtotal;
            _esDescuento = esDescuento;

            TxtTitulo.Text = esDescuento ? "APLICAR DESCUENTO" : "APLICAR RECARGO";
            TxtTitulo.Foreground = esDescuento ?
                new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(230, 126, 34)) :
                new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(39, 174, 96));

            TxtInfo.Text = $"Subtotal actual: {subtotal:C}";
            TxtValor.Focus();
            TxtValor.TextChanged += TxtValor_TextChanged;
        }

        private void RbMetodo_Checked(object sender, RoutedEventArgs e)
        {
            if (TxtEtiqueta == null) return;

            if (RbPorcentaje.IsChecked == true)
            {
                TxtEtiqueta.Text = "Ingrese el porcentaje:";
            }
            else
            {
                TxtEtiqueta.Text = "Ingrese el monto fijo:";
            }

            TxtValor.Clear();
            ActualizarCalculos();
        }

        private void TxtValor_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Solo permitir números y coma decimal
            Regex regex = new Regex(@"^[0-9,]+$");
            e.Handled = !regex.IsMatch(e.Text);
        }

        private void TxtValor_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ActualizarCalculos();
        }

        private void ActualizarCalculos()
        {
            if (TxtValor == null || TxtCalculado == null) return;

            if (!decimal.TryParse(TxtValor.Text, out decimal valor) || valor < 0)
            {
                TxtCalculado.Text = "Monto calculado: $0.00";
                return;
            }

            decimal montoAjuste = 0;

            if (RbPorcentaje.IsChecked == true)
            {
                // Calcular porcentaje
                if (valor > 100)
                {
                    TxtCalculado.Text = "El porcentaje no puede ser mayor a 100%";
                    return;
                }
                montoAjuste = _subtotal * (valor / 100);
            }
            else
            {
                // Monto fijo
                montoAjuste = valor;
            }

            // Validar que el descuento no sea mayor al subtotal
            if (_esDescuento && montoAjuste > _subtotal)
            {
                TxtCalculado.Text = "El descuento no puede ser mayor al subtotal";
                return;
            }

            TxtCalculado.Text = $"Monto calculado: {montoAjuste:C}";
        }

        private void BtnAplicar_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(TxtValor.Text, out decimal valor) || valor <= 0)
            {
                MessageBox.Show(
                    "Debe ingresar un valor válido mayor a cero.",
                    "Validación",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                TxtValor.Focus();
                return;
            }

            decimal montoAjuste = 0;

            if (RbPorcentaje.IsChecked == true)
            {
                if (valor > 100)
                {
                    MessageBox.Show(
                        "El porcentaje no puede ser mayor a 100%.",
                        "Validación",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    TxtValor.Focus();
                    return;
                }
                montoAjuste = _subtotal * (valor / 100);
            }
            else
            {
                montoAjuste = valor;
            }

            // Validar que el descuento no sea mayor al subtotal
            if (_esDescuento && montoAjuste > _subtotal)
            {
                MessageBox.Show(
                    "El descuento no puede ser mayor al subtotal de la venta.",
                    "Validación",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            MontoAjuste = montoAjuste;
            DialogResult = true;
            Close();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F9)
            {
                BtnAplicar_Click(sender, e);
                e.Handled = true;
            }
        }
    }
}