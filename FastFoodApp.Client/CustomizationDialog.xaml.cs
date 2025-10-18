using System.Windows;
using System.Windows.Controls;

namespace FastFoodApp.Client
{
    public partial class CustomizationDialog : Window
    {
        public string CustomizacionesTexto { get; private set; } = string.Empty;

        public CustomizationDialog(int productId, string productName)
        {
            InitializeComponent();
            TxtTitulo.Text = $"Personalizar: {productName}";
        }

        private void BtnSugerencia_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                var sugerencia = btn.Content.ToString();

                // Agregar sugerencia al final del texto
                if (!string.IsNullOrWhiteSpace(TxtCustomizaciones.Text))
                    TxtCustomizaciones.Text += "\n";

                TxtCustomizaciones.Text += sugerencia;
            }
        }

        private void BtnAceptar_Click(object sender, RoutedEventArgs e)
        {
            CustomizacionesTexto = TxtCustomizaciones.Text.Trim();
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