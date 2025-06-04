using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Application = Microsoft.Maui.Controls.Application;

namespace BoothDownloadApp.Maui
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            MainPage = new NavigationPage(new MainPage());
        }
    }
}
