using FastFoodApp.Core.Data;
using FastFoodApp.Core.Models;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FastFoodApp.Client
{
    public class PriceListViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public bool IsActive { get; set; }
        public bool IsProtected { get; set; }
        public string EstadoTexto => IsActive ? "Activa" : "Inactiva";
        public string ProtegidaTexto => IsProtected ? "Sí" : "No";
        public Visibility EsEliminableVisibility => IsProtected ? Visibility.Collapsed : Visibility.Visible;
    }

    public partial class PriceListsWindow : Window
    {
        public PriceListsWindow()
        {
            InitializeComponent();
            CargarListas();
        }

        private void BtnAyuda_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "GESTIÓN DE LISTAS DE PRECIOS:\n\n" +
                "1️⃣ LISTAS PROTEGIDAS:\n" +
                "   • General, Mayorista y Minorista son listas del sistema\n" +
                "   • No se pueden eliminar pero sí editar\n" +
                "   • Sirven como base para su negocio\n\n" +
                "2️⃣ CREAR NUEVA LISTA:\n" +
                "   • Haga clic en 'Nueva Lista' o presione F2\n" +
                "   • Defina nombre y descripción\n" +
                "   • Puede activar/desactivar según necesite\n\n" +
                "3️⃣ EDITAR LISTA:\n" +
                "   • Haga clic en '✏️ Editar' para modificar\n" +
                "   • Puede cambiar nombre, descripción y estado\n\n" +
                "4️⃣ ELIMINAR LISTA:\n" +
                "   • Solo disponible para listas personalizadas\n" +
                "   • Las listas protegidas no se pueden eliminar\n\n" +
                "5️⃣ ASIGNAR PRECIOS:\n" +
                "   • Los precios se asignan desde la edición de productos\n" +
                "   • Cada producto puede tener diferentes precios por lista\n\n" +
                "⌨️ ATAJOS DE TECLADO:\n" +
                "   F1 - Esta ayuda\n" +
                "   F2 - Nueva lista\n" +
                "   Esc - Cerrar",
                "Ayuda - Listas de Precios",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void CargarListas()
        {
            using var ctx = new AppDbContext();
            var listas = ctx.PriceLists
                .OrderBy(pl => pl.Name)
                .Select(pl => new PriceListViewModel
                {
                    Id = pl.Id,
                    Name = pl.Name,
                    Description = pl.Description,
                    IsActive = pl.IsActive,
                    IsProtected = pl.IsProtected
                })
                .ToList();

            DgListas.ItemsSource = listas;
        }

        private void BtnNuevo_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new PriceListDialog();
            if (dialog.ShowDialog() == true)
            {
                CargarListas();
            }
        }

        private void BtnEditar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is PriceListViewModel lista)
            {
                var dialog = new PriceListDialog(lista.Id);
                if (dialog.ShowDialog() == true)
                {
                    CargarListas();
                }
            }
        }

        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is PriceListViewModel lista)
            {
                if (lista.IsProtected)
                {
                    MessageBox.Show(
                        "No se puede eliminar una lista de precios protegida del sistema.",
                        "Operación no permitida",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    $"¿Está seguro que desea eliminar la lista '{lista.Name}'?\n\n" +
                    "Esta acción no se puede deshacer.",
                    "Confirmar eliminación",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using var ctx = new AppDbContext();
                        var priceList = ctx.PriceLists.Find(lista.Id);

                        if (priceList != null)
                        {
                            ctx.PriceLists.Remove(priceList);
                            ctx.SaveChanges();

                            MessageBox.Show(
                                "Lista de precios eliminada correctamente.",
                                "Éxito",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);

                            CargarListas();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Error al eliminar la lista de precios:\n{ex.Message}",
                            "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
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

                case Key.F2:
                    BtnNuevo_Click(sender, e);
                    e.Handled = true;
                    break;
            }
        }
    }
}