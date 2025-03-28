using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MessageBox = System.Windows.MessageBox; // WPF の MessageBox を明示的に指定
using System.Windows.Forms;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Net.Http;



namespace BoothDownloadApp
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true, PropertyNameCaseInsensitive = true };

        public ObservableCollection<BoothItem> Items { get; set; } = new ObservableCollection<BoothItem>();

        private int _progress;
        public int Progress
        {
            get => _progress;
            set
            {
                if (_progress != value)
                {
                    _progress = value;
                    OnPropertyChanged();
                }
            }
        }

        // 管理用JSONファイルのパス（例：アプリケーションディレクトリ直下）
        private readonly string manageFilePath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "booth_manage.json");

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            // 起動時に管理用JSONファイルから読み込み（なければ作成）
            LoadManagementData();
        }

        /// <summary>
        /// 管理用JSONファイル（booth_manage.json）からデータを読み込み、Itemsに反映する。
        /// ファイルが存在しなければ空の内容で作成する。
        /// </summary>
        private void LoadManagementData()
        {
            if (!File.Exists(manageFilePath))
            {
                var emptyLibrary = new BoothLibrary { Library = new List<BoothItem>() };
                string defaultJson = JsonSerializer.Serialize(emptyLibrary, JsonSerializerOptions);
                File.WriteAllText(manageFilePath, defaultJson);
            }

            try
            {
                string json = File.ReadAllText(manageFilePath);
                var boothLibrary = JsonSerializer.Deserialize<BoothLibrary>(json, JsonSerializerOptions);
                if (boothLibrary?.Library != null)
                {
                    Items.Clear();
                    foreach (var item in boothLibrary.Library)
                    {
                        Items.Add(item);
                    }
                }
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"管理用JSONの読み込みエラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 管理用JSONファイルへItemsの内容を保存する
        /// </summary>
        private void SaveManagementData()
        {
            var boothLibrary = new BoothLibrary { Library = Items.ToList() };
            string json = JsonSerializer.Serialize(boothLibrary, JsonSerializerOptions);
            File.WriteAllText(manageFilePath, json);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            SaveManagementData();
            base.OnClosing(e);
        }

        private async void StartDownload(object sender, RoutedEventArgs e)
        {
            if (Items.Count == 0)
            {
                MessageBox.Show("ダウンロードするアイテムがありません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 選択された商品を取得
            var selectedItems = Items.Where(item => item.IsSelected || item.Downloads.Any(d => d.IsSelected)).ToList();

            if (selectedItems.Count == 0)
            {
                MessageBox.Show("ダウンロードするアイテムを選択してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // ダウンロード処理の開始
            Progress = 0;
            int totalFiles = selectedItems.Sum(item => item.Downloads.Count(d => d.IsSelected));

            using HttpClient httpClient = new HttpClient();
            int downloadedFiles = 0;

            foreach (var item in selectedItems)
            {
                string shopFolder = Path.Combine(DownloadFolderPath, item.ShopName);
                string productFolder = Path.Combine(shopFolder, item.ProductName);

                // フォルダを作成
                Directory.CreateDirectory(productFolder);

                foreach (var file in item.Downloads.Where(d => d.IsSelected))
                {
                    string filePath = Path.Combine(productFolder, file.FileName);

                    try
                    {
                        using HttpResponseMessage response = await httpClient.GetAsync(file.DownloadLink);
                        response.EnsureSuccessStatusCode();
                        await using FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                        await response.Content.CopyToAsync(fs);

                        downloadedFiles++;
                        Progress = (int)((double)downloadedFiles / totalFiles * 100);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"ダウンロード失敗: {file.FileName}\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }

            MessageBox.Show("ダウンロードが完了しました！", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
        }



        private void StopDownload(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("ダウンロード停止！（機能未実装）", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoadJsonData(object sender, RoutedEventArgs e)
        {
            string sourcePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "booth_data.json");
            string targetDirectory = "C:\\BoothData";
            string targetPath = Path.Combine(targetDirectory, "booth_data.json");

            try
            {
                if (File.Exists(sourcePath))
                {
                    if (!Directory.Exists(targetDirectory))
                    {
                        Directory.CreateDirectory(targetDirectory);
                    }
                    File.Copy(sourcePath, targetPath, true);
                }

                if (File.Exists(targetPath))
                {
                    string json = File.ReadAllText(targetPath);
                    var boothLibrary = JsonSerializer.Deserialize<BoothLibrary>(json, JsonSerializerOptions);

                    if (boothLibrary?.Library != null)
                    {
                        Items.Clear();
                        foreach (var item in boothLibrary.Library)
                        {
                            Items.Add(item);
                        }
                    }
                    MessageBox.Show("JSON データを読み込みました！", "情報", MessageBoxButton.OK, MessageBoxImage.Information);

                    // 読み込み後、管理用JSONに保存して内容を反映
                    SaveManagementData();
                }
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"JSON パースエラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"処理中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region INotifyPropertyChanged 実装
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null!)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        private void ListView_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // 必要に応じて選択変更時の処理を追加
        }

        private string _downloadFolderPath = "C:\\BoothData"; // デフォルトフォルダ

        public string DownloadFolderPath
        {
            get => _downloadFolderPath;
            set
            {
                if (_downloadFolderPath != value)
                {
                    _downloadFolderPath = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// フォルダ選択ダイアログを開く
        /// </summary>
        private void SelectDownloadFolder(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true, // フォルダ選択モード
                InitialDirectory = DownloadFolderPath, // 初期フォルダ
                Title = "ダウンロードフォルダを選択してください"
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                DownloadFolderPath = dialog.FileName;
            }
        }
    }

    public class BoothLibrary
    {
        [JsonPropertyName("library")]
        public List<BoothItem> Library { get; set; } = new List<BoothItem>();
    }


    public class BoothItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        private string _description = string.Empty; // Initialize _description to a non-null value

        [JsonPropertyName("productName")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("shopName")]
        public string ShopName { get; set; } = string.Empty;

        [JsonPropertyName("thumbnail")]
        public string Thumbnail { get; set; } = string.Empty;

        [JsonPropertyName("downloads")]
        public List<DownloadInfo> Downloads { get; set; } = new List<DownloadInfo>();

        // 商品単位の選択状態
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                    if (Downloads != null)
                    {
                        foreach (var download in Downloads)
                        {
                            download.IsSelected = value;
                        }
                    }
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null!)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // 内部クラス DownloadInfo
        public class DownloadInfo : INotifyPropertyChanged
        {
            private bool _isSelected;
            private string _description = string.Empty; // Initialize _description to a non-null value

            [JsonPropertyName("fileName")]
            public string FileName { get; set; } = string.Empty;

            [JsonPropertyName("downloadLink")]
            public string DownloadLink { get; set; } = string.Empty;

            [JsonPropertyName("description")]
            public string Description
            {
                get => _description;
                set
                {
                    if (_description != value)
                    {
                        _description = value;
                        OnPropertyChanged();
                    }
                }
            }

            public bool IsSelected
            {
                get => _isSelected;
                set
                {
                    if (_isSelected != value)
                    {
                        _isSelected = value;
                        OnPropertyChanged();
                    }
                }
            }

            public event PropertyChangedEventHandler? PropertyChanged;

            protected void OnPropertyChanged([CallerMemberName] string propertyName = null!)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        

        
    }


}
