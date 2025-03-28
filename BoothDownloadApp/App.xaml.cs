using System.Windows;

namespace BoothDownloadApp
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // ダークモードのデフォルト設定など
            ThemeManager.ToggleDarkMode(false);
        }
    }
}
