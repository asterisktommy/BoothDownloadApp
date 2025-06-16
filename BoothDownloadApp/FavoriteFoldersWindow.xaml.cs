using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace BoothDownloadApp
{
    public partial class FavoriteFoldersWindow : Window
    {
        public string[] FolderNames { get; private set; }

        public FavoriteFoldersWindow(string[] names)
        {
            InitializeComponent();
            FolderNames = names.ToArray();
            for (int i = 0; i < FolderNames.Length; i++)
            {
                var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0,2,0,2) };
                sp.Children.Add(new TextBlock { Text = $"{i}", Width = 20 });
                sp.Children.Add(new TextBox { Text = FolderNames[i], Width = 200, Name = $"Box{i}" });
                Items.Items.Add(sp);
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < FolderNames.Length; i++)
            {
                if (Items.Items[i] is StackPanel sp && sp.Children[1] is TextBox tb)
                {
                    FolderNames[i] = tb.Text;
                }
            }
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
