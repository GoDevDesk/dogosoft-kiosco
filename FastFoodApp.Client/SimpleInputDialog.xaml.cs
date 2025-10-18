using System.Windows;

namespace FastFoodApp.Client
{
    public partial class SimpleInputDialog : Window
    {
        public string ResultText { get; private set; }

        public SimpleInputDialog(string message)
        {
            InitializeComponent();
            TxtMessage.Text = message;
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            ResultText = InputBox.Text;
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public string ShowDialogAndReturn()
        {
            return ShowDialog() == true ? ResultText : null;
        }
    }
}
