using System.Threading.Tasks;
using System.Windows;

namespace BoothDownloadApp
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            bool loggedIn = await LoginManager.IsLoggedInAsync();
            if (!loggedIn)
            {
                var loginWindow = new LoginWindow();
                loginWindow.ShowDialog();
            }

            MainWindow mainWindow = new MainWindow();
            MainWindow = mainWindow;
            mainWindow.Show();
        }
    }
}
