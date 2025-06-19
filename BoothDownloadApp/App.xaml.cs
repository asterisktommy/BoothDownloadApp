using System.Windows;

namespace BoothDownloadApp
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var loginWindow = new LoginWindow();
            loginWindow.ShowDialog();

            MainWindow mainWindow = new MainWindow();
            MainWindow = mainWindow;
            mainWindow.Show();
        }
    }
}
