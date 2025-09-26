using KioscoApp.Core.Data;
using KioscoApp.Core.Models;
using Microsoft.Win32;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace KioscoApp.Client
{
    public partial class ProductDialog : Window
    {
        private readonly Product _product; // producto a editar o nuevo

        public ProductDialog(Product product = null)
        {
            InitializeComponent();

            using var ctx = new AppDbContext();

            if (product != null)
            {
                // Editar producto existente
                _product = ctx.Products.FirstOrDefault(p => p.Id == product.Id);
                if (_product != null)
                {
                    TxtCode.Text = _product.Code;
                    TxtName.Text = _product.Name;
                    TxtPrice.Text = _product.Price.ToString();
                    TxtStock.Text = _product.Stock.ToString();
                    TxtCategory.Text = _product.Category;

                    // Mostrar foto si existe
                    if (!string.IsNullOrEmpty(_product.ImagePath) && File.Exists(_product.ImagePath))
                    {
                        ImgProduct.Source = new BitmapImage(new System.Uri(_product.ImagePath));
                    }
                }
            }
            else
            {
                // Crear nuevo producto
                _product = new Product();
            }
        }

        private void BtnAceptar_Click(object sender, RoutedEventArgs e)
        {
            // Validar y asignar campos
            if (string.IsNullOrWhiteSpace(TxtName.Text))
            {
                MessageBox.Show("El nombre es obligatorio.");
                return;
            }

            _product.Code = TxtCode.Text;
            _product.Name = TxtName.Text;
            if (decimal.TryParse(TxtPrice.Text, out var price))
                _product.Price = price;
            else
                _product.Price = 0;

            if (int.TryParse(TxtStock.Text, out var stock))
                _product.Stock = stock;
            else
                _product.Stock = 0;

            _product.Category = TxtCategory.Text;

            using var ctx = new AppDbContext();

            if (_product.Id == 0)
            {
                // Nuevo producto
                ctx.Products.Add(_product);
            }
            else
            {
                // Producto existente: actualizar
                var p = ctx.Products.FirstOrDefault(x => x.Id == _product.Id);
                if (p != null)
                {
                    p.Code = _product.Code;
                    p.Name = _product.Name;
                    p.Price = _product.Price;
                    p.Stock = _product.Stock;
                    p.Category = _product.Category;
                    p.ImagePath = _product.ImagePath; // actualizar foto
                }
            }

            ctx.SaveChanges();
            DialogResult = true;
            Close();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnCargarFoto_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Seleccionar foto",
                Filter = "Archivos de imagen|*.jpg;*.jpeg;*.png;*.bmp"
            };

            if (dlg.ShowDialog() == true)
            {
                _product.ImagePath = dlg.FileName; // guardar ruta
                ImgProduct.Source = new BitmapImage(new System.Uri(dlg.FileName));
            }
        }
    }
}
