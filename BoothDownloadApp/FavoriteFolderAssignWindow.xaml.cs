using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace BoothDownloadApp
{
    public partial class FavoriteFolderAssignWindow : Window
    {
        public IList<string> FolderNames { get; }
        public BoothItem Item { get; }
        public int ItemFolderIndex { get; set; }

        public FavoriteFolderAssignWindow(BoothItem item, IList<string> folderNames)
        {
            InitializeComponent();
            Item = item;
            FolderNames = folderNames;
            ItemFolderIndex = item.FavoriteFolderIndex;
            DataContext = this;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Item.FavoriteFolderIndex = ItemFolderIndex;
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
