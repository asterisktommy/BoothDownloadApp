using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace BoothDownloadApp
{
    public partial class FavoriteTagsWindow : Window
    {
        private readonly List<CheckBox> _boxes = new();
        public IReadOnlyList<string> SelectedTags { get; private set; } = new List<string>();

        public FavoriteTagsWindow(IEnumerable<string> tags, IEnumerable<string> favorites)
        {
            InitializeComponent();
            foreach (var tag in tags)
            {
                var cb = new CheckBox { Content = tag, Margin = new Thickness(0, 2, 0, 2), IsChecked = favorites.Contains(tag) };
                TagsPanel.Children.Add(cb);
                _boxes.Add(cb);
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedTags = _boxes.Where(b => b.IsChecked == true).Select(b => b.Content?.ToString() ?? string.Empty).ToList();
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
