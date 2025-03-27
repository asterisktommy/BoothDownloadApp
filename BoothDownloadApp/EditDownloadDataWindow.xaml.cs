using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using static BoothDownloadApp.BoothItem;

namespace BoothDownloadApp
{
    public partial class EditDownloadDataWindow : Window
    {
        // 編集対象のDownloadInfoのコレクション（ここではサンプルとして単一の商品内のDownloadInfoのリストを想定）
        public ObservableCollection<DownloadInfo> DownloadItems { get; set; }

        // JSONファイルのパス（例として固定パス）
        private readonly string editDataFilePath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "editDownloadData.json");

        public EditDownloadDataWindow(ObservableCollection<DownloadInfo> items)
        {
            InitializeComponent();
            DownloadItems = items;
            dataGrid.ItemsSource = DownloadItems;
            // JSONファイルが存在すれば読み込み
            if (File.Exists(editDataFilePath))
            {
                try
                {
                    string json = File.ReadAllText(editDataFilePath);
                    var loadedItems = JsonSerializer.Deserialize<ObservableCollection<DownloadInfo>>(json);
                    if (loadedItems != null)
                    {
                        DownloadItems = loadedItems;
                        dataGrid.ItemsSource = DownloadItems;
                    }
                }
                catch { /* エラーハンドリング */ }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string json = JsonSerializer.Serialize(DownloadItems, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(editDataFilePath, json);
                MessageBox.Show("保存しました！", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"保存エラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
