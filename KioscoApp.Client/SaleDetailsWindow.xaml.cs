using KioscoApp.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace KioscoApp.Client
{
    public partial class SaleDetailsWindow : Window
    {
        public SaleDetailsWindow(List<SaleItem> items)
        {
            InitializeComponent();
            GridItems.ItemsSource = items;

            // Calcular total y mostrarlo
            var total = items.Sum(i => i.Subtotal);
            TxtTotal.Text = total.ToString("C");
        }
    }
}
