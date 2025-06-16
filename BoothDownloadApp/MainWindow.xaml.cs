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



namespace BoothDownloadApp
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true, PropertyNameCaseInsensitive = true };

        public ObservableCollection<BoothItem> Items { get; set; } = new ObservableCollection<BoothItem>();
        private ICollectionView ItemsView => CollectionViewSource.GetDefaultView(Items);

        private readonly DatabaseManager _dbManager = new DatabaseManager(Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "download_history.db"));
        private readonly Settings _settings = SettingsManager.Load();


        private bool _showOnlyFavorites;
        public bool ShowOnlyFavorites
        {
            get => _showOnlyFavorites;
            set
            {
                if (_showOnlyFavorites != value)
                {
                    _showOnlyFavorites = value;
                    OnPropertyChanged();
                    ApplyFilters();
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

        private readonly ObservableCollection<string> _favoriteTags = new ObservableCollection<string>();
        public ObservableCollection<string> FavoriteTags => _favoriteTags;

        public ObservableCollection<string> FavoriteFolderNames { get; } = new ObservableCollection<string>();

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

        private string _searchQuery = string.Empty;
        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (_searchQuery != value)
                {
                    _searchQuery = value;
                    OnPropertyChanged();
                    ApplyFilters();
                }
            }
        }

        private int _selectedFavoriteFolderIndex = -1;
        public int SelectedFavoriteFolderIndex
        {
            get => _selectedFavoriteFolderIndex;
            set
            {
                if (_selectedFavoriteFolderIndex != value)
                {
                    _selectedFavoriteFolderIndex = value;
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
            DownloadFolderPath = string.IsNullOrWhiteSpace(_settings.DownloadPath) ? "C:\\BoothData" : _settings.DownloadPath;
            if (_settings.FavoriteTags != null)
            {
                foreach (var t in _settings.FavoriteTags)
                {
                    _favoriteTags.Add(t);
                }
            }
            if (_settings.FavoriteFolders != null)
            {
                foreach (var n in _settings.FavoriteFolders)
                {
                    FavoriteFolderNames.Add(n);
                }
            }
            // 起動後に管理用JSONを読み込む
            Loaded += async (_, __) => await LoadManagementDataAsync();
        }

        /// <summary>
        /// 管理用JSONファイル（booth_manage.json）からデータを読み込み、Itemsに反映する。
        /// ファイルが存在しなければ空の内容で作成する。
        /// </summary>
        private async Task LoadManagementDataAsync()
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
                    await FetchMissingTagsAsync();
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
            _settings.DownloadPath = DownloadFolderPath;
            _settings.FavoriteTags = _favoriteTags.ToList();
            _settings.FavoriteFolders = FavoriteFolderNames.ToArray();
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

            var selectedItems = Items.Where(item => item.IsSelected || item.Downloads.Any(d => d.IsSelected)).ToList();

            if (selectedItems.Count == 0)
            {
                MessageBox.Show("ダウンロードするアイテムを選択してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await StartDownloadAsync(selectedItems, d => d.IsSelected);
        }

        private async void DownloadAllNotDownloaded(object sender, RoutedEventArgs e)
        {
            if (_isDownloading)
            {
                MessageBox.Show("既にダウンロード処理が実行されています。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            UpdateDownloadStatus();
            var targetItems = Items.Where(i => i.Downloads.Any(d => !d.IsDownloaded)).ToList();

            if (targetItems.Count == 0)
            {
                MessageBox.Show("未ダウンロードのアイテムはありません。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            await StartDownloadAsync(targetItems, d => !d.IsDownloaded);
        }

        private async Task StartDownloadAsync(List<BoothItem> items, Func<BoothItem.DownloadInfo, bool> fileSelector)
        {
            Progress = 0;
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            _isDownloading = true;

            try
            {
                await DownloadService.DownloadItemsAsync(
                    items,
                    fileSelector,
                    DownloadFolderPath,
                    _settings.RetryCount,
                    _dbManager,
                    _settings.FavoriteFolders,
                    new Progress<int>(p => Progress = p),
                    token);
                MessageBox.Show("ダウンロードが完了しました！", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("ダウンロードをキャンセルしました。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            finally
            {
                _isDownloading = false;
                _cts = null;
            }
        }

        private void StopDownload(object sender, RoutedEventArgs e)
        {
            if (_isDownloading && _cts != null)
            {
                _cts.Cancel();
            }
        }

        private async void LoadJsonData(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "booth_data.json を選択してください"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                string json = File.ReadAllText(dialog.FileName);
                var items = JsonLoader.LoadItems(json, JsonSerializerOptions);

                if (items != null)
                {
                    Items.Clear();
                    foreach (var item in items)
                    {
                        Items.Add(item);
                    }
                    UpdateDownloadStatus();
                    await FetchMissingTagsAsync();
                }

                MessageBox.Show("JSON データを読み込みました！", "情報", MessageBoxButton.OK, MessageBoxImage.Information);

                // 読み込み後、管理用JSONに保存して内容を反映
                SaveManagementData();
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
                    string path = Path.Combine(
                        DownloadFolderPath,
                        PathUtils.Sanitize(item.ShopName),
                        PathUtils.Sanitize(item.ProductName),
                        PathUtils.Sanitize(download.FileName));
                    download.IsDownloaded = File.Exists(path);
                }
                item.IsDownloaded = item.Downloads.All(d => d.IsDownloaded);

                if (item.FavoriteFolderIndex >= 0 && item.FavoriteFolderIndex < _settings.FavoriteFolders.Length)
                {
                    string favRoot = _settings.FavoriteFolders[item.FavoriteFolderIndex];
                    bool allCopied = true;
                    foreach (var d in item.Downloads)
                    {
                        string p = Path.Combine(
                            favRoot,
                            PathUtils.Sanitize(item.ShopName),
                            PathUtils.Sanitize(item.ProductName),
                            PathUtils.Sanitize(d.FileName));
                        if (!File.Exists(p))
                        {
                            allCopied = false;
                            break;
                        }
                    }
                    if (allCopied)
                    {
                        item.CopiedFavoriteFolderIndex = item.FavoriteFolderIndex;
                        item.CopiedFavoriteFolderName = favRoot;
                    }
                    else
                    {
                        item.CopiedFavoriteFolderIndex = -1;
                        item.CopiedFavoriteFolderName = string.Empty;
                    }
                }
                else
                {
                    item.CopiedFavoriteFolderIndex = -1;
                    item.CopiedFavoriteFolderName = string.Empty;
                }
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
            foreach (var f in _favoriteTags.ToList())
            {
                if (!tags.Contains(f))
                {
                    _favoriteTags.Remove(f);
                }
            }
        }

        private async Task FetchMissingTagsAsync()
        {
            var targets = Items.Where(i => !i.TagsFetched && !string.IsNullOrWhiteSpace(i.ItemUrl)).ToList();
            if (targets.Count == 0) return;

            int index = 0;
            int concurrency = 5;
            var tasks = Enumerable.Range(0, concurrency).Select(async _ =>
            {
                while (true)
                {
                    BoothItem? item;
                    lock (targets)
                    {
                        if (index >= targets.Count) return;
                        item = targets[index++];
                    }

                    var fetched = await ProductFetcher.FetchItemAsync(item.ItemUrl);
                    if (fetched != null)
                    {
                        item.Tags = fetched.Tags;
                        item.TagsFetched = true;
                    }
                }
            });

            await Task.WhenAll(tasks);
            UpdateAvailableTags();
            ApplyFilters();
            SaveManagementData();
        }

        private void ApplyFilters()
        {
            if (ItemsView == null) return;

            ItemsView.Filter = obj =>
            {
                if (obj is not BoothItem item) return false;
                return FilterManager.Matches(item, ShowOnlyNotDownloaded, SelectedTag, ShowOnlyUpdates, SearchQuery, ShowOnlyFavorites, FavoriteTags, SelectedFavoriteFolderIndex);
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

        private void OpenManualAdd(object sender, RoutedEventArgs e)
        {
            var window = new ManualAddWindow();
            if (window.ShowDialog() == true && window.ResultItem != null)
            {
                var item = window.ResultItem;
                foreach (var path in window.SelectedFilePaths)
                {
                    string dest = Path.Combine(
                        DownloadFolderPath,
                        PathUtils.Sanitize(item.ShopName),
                        PathUtils.Sanitize(item.ProductName),
                        PathUtils.Sanitize(Path.GetFileName(path)));
                    Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                    try
                    {
                        File.Copy(path, dest, true);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"ファイルコピーに失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                Items.Add(item);
                SaveManagementData();
            UpdateDownloadStatus();
        }

        private void OpenFavoriteFolderSetting(object sender, RoutedEventArgs e)
        {
            var window = new FavoriteFoldersWindow(FavoriteFolderNames.ToArray());
            if (window.ShowDialog() == true)
            {
                FavoriteFolderNames.Clear();
                foreach (var n in window.FolderNames)
                {
                    FavoriteFolderNames.Add(n);
                }
            }
        }

        private void SetFavoriteFolder(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && int.TryParse(mi.Tag?.ToString(), out int idx))
            {
                if (mi.CommandParameter is BoothItem item)
                {
                    item.FavoriteFolderIndex = idx;
                    SaveManagementData();
                    UpdateDownloadStatus();
                }
            }
        }

        private void ClearFavoriteFolder(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.CommandParameter is BoothItem item)
            {
                item.FavoriteFolderIndex = -1;
                SaveManagementData();
                UpdateDownloadStatus();
            }
        }

        private void OpenFavoritesSetting(object sender, RoutedEventArgs e)
        {
            var tags = _availableTags.Where(t => t != "All");
            var window = new FavoriteTagsWindow(tags, _favoriteTags);
            if (window.ShowDialog() == true)
            {
                _favoriteTags.Clear();
                foreach (var t in window.SelectedTags)
                {
                    _favoriteTags.Add(t);
                }
                ApplyFilters();
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
