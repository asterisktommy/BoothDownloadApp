using Android.App;
using Android.Content.PM;
using Microsoft.Maui;

namespace BoothDownloadApp.Maui
{
    [Activity(Label = "BoothDownloadApp", Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode)]
    public class MainActivity : MauiAppCompatActivity
    {
    }
}
