using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace BoothDownloadApp.Maui
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>();

            return builder.Build();
        }
    }
}
