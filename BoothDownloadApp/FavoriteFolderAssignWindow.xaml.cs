using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace BoothDownloadApp
{
    public partial class FavoriteFolderAssignWindow : Window
    {
        public IList<string> FolderNames { get; }
        public IList<string> FolderNamesWithNone { get; }
        public BoothItem Item { get; }
        public int SelectedIndex { get; set; }

        public FavoriteFolderAssignWindow(BoothItem item, IList<string> folderNames)
        {
            InitializeComponent();
            Item = item;
            FolderNames = folderNames;
            FolderNamesWithNone = new[] { "未選択" }.Concat(folderNames).ToList();
            SelectedIndex = item.FavoriteFolderIndex + 1;
            DataContext = this;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Item.FavoriteFolderIndex = SelectedIndex - 1;
            foreach (var d in Item.Downloads)
            {
                // leave individual index as is if user didn't change
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
