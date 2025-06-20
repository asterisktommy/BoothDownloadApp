using System.Diagnostics;
using System.Windows;

namespace BoothDownloadApp
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                Process.Start(new ProcessStartInfo(
                    "https://accounts.booth.pm/users/sign_in") { UseShellExecute = true });
            }
            catch { }

            MainWindow mainWindow = new MainWindow();
            MainWindow = mainWindow;
            mainWindow.Show();
        }
    }
}
