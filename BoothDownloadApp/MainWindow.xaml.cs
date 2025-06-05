using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using MessageBox = System.Windows.MessageBox; // WPF の MessageBox を明示的に指定
using System.Windows.Forms;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Net.Http;
using System.Linq;
using System.Windows.Input;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Controls;
using Microsoft.VisualBasic;



namespace BoothDownloadApp
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true, PropertyNameCaseInsensitive = true };

        public ObservableCollection<BoothItem> Items { get; set; } = new ObservableCollection<BoothItem>();
        private ICollectionView ItemsView => CollectionViewSource.GetDefaultView(Items);

        private readonly DatabaseManager _dbManager = new DatabaseManager(Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "download_history.db"));
        private readonly Settings _settings = SettingsManager.Load();

        private bool _isDarkMode;
        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                if (_isDarkMode != value)
                {
                    _isDarkMode = value;
                    OnPropertyChanged();
                    ThemeManager.ToggleDarkMode(value);
                }
            }
        }

        private bool _showOnlyNotDownloaded;
        public bool ShowOnlyNotDownloaded
        {
            get => _showOnlyNotDownloaded;
            set
            {
                if (_showOnlyNotDownloaded != value)
                {
                    _showOnlyNotDownloaded = value;
                    OnPropertyChanged();
                    ApplyFilters();
                }
            }
        }

        private bool _showOnlyUpdates;
        public bool ShowOnlyUpdates
        {
            get => _showOnlyUpdates;
            set
            {
                if (_showOnlyUpdates != value)
                {
                    _showOnlyUpdates = value;
                    OnPropertyChanged();
                    ApplyFilters();
                }
            }
        }

        private readonly ObservableCollection<string> _availableTags = new ObservableCollection<string>();
        public ObservableCollection<string> AvailableTags => _availableTags;

        private string? _selectedTag = "All";
        public string? SelectedTag
        {
            get => _selectedTag;
            set
            {
                if (_selectedTag != value)
                {
                    _selectedTag = value;
                    OnPropertyChanged();
                    ApplyFilters();
                }
            }
        }

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

        private CancellationTokenSource? _cts;
        private bool _isDownloading;

        public ICommand OpenLinkCommand { get; }

        // 管理用JSONファイルのパス（例：アプリケーションディレクトリ直下）
        private readonly string manageFilePath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "booth_manage.json");

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            OpenLinkCommand = new RelayCommand(OpenLink);
            _isDownloading = false;
            // apply settings
            IsDarkMode = _settings.DarkMode;
            DownloadFolderPath = string.IsNullOrWhiteSpace(_settings.DownloadPath) ? "C:\\BoothData" : _settings.DownloadPath;
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
                var emptyLibrary = new BoothLibrary { Library = new List<BoothItem>(), Gifts = new List<BoothItem>() };
                string defaultJson = JsonSerializer.Serialize(emptyLibrary, JsonSerializerOptions);
                File.WriteAllText(manageFilePath, defaultJson);
            }

            try
            {
                string json = File.ReadAllText(manageFilePath);
                var boothLibrary = JsonSerializer.Deserialize<BoothLibrary>(json, JsonSerializerOptions);
                if (boothLibrary != null)
                {
                    Items.Clear();
                    if (boothLibrary.Library != null)
                    {
                        foreach (var item in boothLibrary.Library)
                        {
                            Items.Add(item);
                        }
                    }
                    if (boothLibrary.Gifts != null)
                    {
                        foreach (var item in boothLibrary.Gifts)
                        {
                            Items.Add(item);
                        }
                    }
                    UpdateDownloadStatus();
                    ApplyFilters();
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
            var boothLibrary = new BoothLibrary { Library = Items.ToList(), Gifts = new List<BoothItem>() };
            string json = JsonSerializer.Serialize(boothLibrary, JsonSerializerOptions);
            File.WriteAllText(manageFilePath, json);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            SaveManagementData();
            _settings.DarkMode = IsDarkMode;
            _settings.DownloadPath = DownloadFolderPath;
            SettingsManager.Save(_settings);
            base.OnClosing(e);
        }

        private async void StartDownload(object sender, RoutedEventArgs e)
        {
            if (_isDownloading)
            {
                MessageBox.Show("既にダウンロード処理が実行されています。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

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

            UpdateDownloadStatus();

            using HttpClient httpClient = new HttpClient();
            int downloadedFiles = 0;
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            _isDownloading = true;

            foreach (var item in selectedItems)
            {
                string shopFolder = Path.Combine(DownloadFolderPath, item.ShopName);
                string productFolder = Path.Combine(shopFolder, item.ProductName);

                // フォルダを作成
                Directory.CreateDirectory(productFolder);

                foreach (var file in item.Downloads.Where(d => d.IsSelected))
                {
                    string filePath = Path.Combine(productFolder, file.FileName);

                    int attempts = 0;
                    while (true)
                    {
                        try
                        {
                            using HttpResponseMessage response = await httpClient.GetAsync(file.DownloadLink, token);
                            response.EnsureSuccessStatusCode();
                            await using FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                            await response.Content.CopyToAsync(fs, token);

                            downloadedFiles++;
                            Progress = (int)((double)downloadedFiles / totalFiles * 100);
                            file.IsDownloaded = true;
                            _dbManager.SaveHistoryItem(file.FileName, file.DownloadLink);
                            break;
                        }
                        catch (OperationCanceledException)
                        {
                            MessageBox.Show("ダウンロードをキャンセルしました。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }
                        catch (Exception ex)
                        {
                            attempts++;
                            if (attempts > _settings.RetryCount)
                            {
                                MessageBox.Show($"ダウンロード失敗: {file.FileName}\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                                break;
                            }
                            await Task.Delay(1000, token);
                        }
                    }
                }
                item.IsDownloaded = item.Downloads.All(d => d.IsDownloaded);
            }
            _isDownloading = false;
            _cts = null;
            MessageBox.Show("ダウンロードが完了しました！", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
        }



        private void StopDownload(object sender, RoutedEventArgs e)
        {
            if (_isDownloading && _cts != null)
            {
                _cts.Cancel();
            }
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

                    if (boothLibrary != null)
                    {
                        Items.Clear();
                        if (boothLibrary.Library != null)
                        {
                            foreach (var item in boothLibrary.Library)
                            {
                                Items.Add(item);
                            }
                        }
                        if (boothLibrary.Gifts != null)
                        {
                            foreach (var item in boothLibrary.Gifts)
                            {
                                Items.Add(item);
                            }
                        }
                        UpdateDownloadStatus();
                        ApplyFilters();
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

        private async void AddItemFromUrl(object sender, RoutedEventArgs e)
        {
            string url = Interaction.InputBox("商品URLを入力してください", "URL入力", "");
            if (string.IsNullOrWhiteSpace(url)) return;

            try
            {
                var item = await ProductFetcher.FetchItemAsync(url);
                if (item != null)
                {
                    Items.Add(item);
                    SaveManagementData();
                    UpdateDownloadStatus();
                    ApplyFilters();
                    MessageBox.Show("商品情報を追加しました。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("商品情報を取得できませんでした。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"取得に失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    UpdateDownloadStatus();
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

        /// <summary>
        /// ダウンロードフォルダをエクスプローラーで開く
        /// </summary>
        private void OpenDownloadFolder(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Directory.Exists(DownloadFolderPath))
                {
                    Process.Start(new ProcessStartInfo("explorer", DownloadFolderPath) { UseShellExecute = true });
                }
                else
                {
                    MessageBox.Show("フォルダが存在しません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"フォルダを開けませんでした: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 全アイテムのダウンロード済み状態を更新する
        /// </summary>
        private void UpdateDownloadStatus()
        {
            foreach (var item in Items)
            {
                foreach (var download in item.Downloads)
                {
                    string path = Path.Combine(DownloadFolderPath, item.ShopName, item.ProductName, download.FileName);
                    download.IsDownloaded = File.Exists(path);
                }
                item.IsDownloaded = item.Downloads.All(d => d.IsDownloaded);
            }
            UpdateAvailableTags();
            ApplyFilters();
        }

        private void UpdateAvailableTags()
        {
            var tags = Items.SelectMany(i => i.Tags).Distinct().OrderBy(t => t).ToList();
            _availableTags.Clear();
            _availableTags.Add("All");
            foreach (var t in tags)
            {
                _availableTags.Add(t);
            }
            if (!_availableTags.Contains(SelectedTag ?? ""))
            {
                SelectedTag = "All";
            }
        }

        private void ApplyFilters()
        {
            if (ItemsView == null) return;

            ItemsView.Filter = obj =>
            {
                if (obj is not BoothItem item) return false;
                return FilterManager.Matches(item, ShowOnlyNotDownloaded, SelectedTag, ShowOnlyUpdates);
            };

            ItemsView.Refresh();
        }

        private void OpenLink(object? parameter)
        {
            if (parameter is string url && !string.IsNullOrWhiteSpace(url))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"リンクを開けませんでした: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void FilterChanged(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void RefreshStatus(object sender, RoutedEventArgs e)
        {
            UpdateDownloadStatus();
        }

        private void OpenEditWindow(object sender, RoutedEventArgs e)
        {
            if (itemsListView.SelectedItem is BoothItem item)
            {
                var list = new ObservableCollection<BoothItem.DownloadInfo>(item.Downloads);
                var window = new EditDownloadDataWindow(list);
                if (window.ShowDialog() == true)
                {
                    item.Downloads = list.ToList();
                    SaveManagementData();
                }
            }
            else
            {
                MessageBox.Show("アイテムを選択してください。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private class RelayCommand : ICommand
        {
            private readonly Action<object?> _execute;
            private readonly Predicate<object?>? _canExecute;

            public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
            {
                _execute = execute;
                _canExecute = canExecute;
            }

            public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
            public void Execute(object? parameter) => _execute(parameter);
            public event EventHandler? CanExecuteChanged { add { } remove { } }
        }
    }

}
