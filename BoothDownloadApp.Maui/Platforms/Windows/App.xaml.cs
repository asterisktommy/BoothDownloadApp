using Microsoft.UI.Xaml;

namespace BoothDownloadApp.Maui
{
    public partial class App : MauiWinUIApplication
    {
        public App() => InitializeComponent();

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
