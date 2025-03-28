using System.Windows;
using System.Windows.Media;

namespace BoothDownloadApp
{
    public class ThemeManager
    {
        public static void ToggleDarkMode(bool isDarkMode)
        {
            if (isDarkMode)
            {
                Application.Current.Resources["WindowBackgroundBrush"] = new SolidColorBrush(Colors.Black);
            }
            else
            {
                Application.Current.Resources["WindowBackgroundBrush"] = new SolidColorBrush(Colors.White);
            }
        }
    }
}
