using System.Windows;
using FastFoodApp.Core.Data;

namespace FastFoodApp.Client
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DatabaseInitializer.EnsureDatabaseAndSeed();
            var main = new MainWindow();
            main.Show();
        }
    }
}