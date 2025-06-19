using Microsoft.Web.WebView2.Core;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;

namespace BoothDownloadApp
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            Loaded += LoginWindow_Loaded;
        }

        private async void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await Browser.EnsureCoreWebView2Async();
            Browser.CoreWebView2.Navigate("https://accounts.booth.pm/users/sign_in");
        }

        protected override async void OnClosed(EventArgs e)
        {
            if (Browser.CoreWebView2 != null)
            {
                var cookieList = await Browser.CoreWebView2.CookieManager.GetCookiesAsync("https://booth.pm/");
                var cookies = cookieList.Select(c => new Cookie(c.Name, c.Value, c.Path, c.Domain));
                await CookieStore.SaveAsync(cookies);
            }
            base.OnClosed(e);
        }
    }
}
