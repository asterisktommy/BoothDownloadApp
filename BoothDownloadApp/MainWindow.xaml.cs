using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using MessageBox = System.Windows.MessageBox; // WPF の MessageBox を明示的に指定
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
using System.Collections.Specialized;



namespace BoothDownloadApp
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true, PropertyNameCaseInsensitive = true };

        public ObservableCollection<BoothItem> Items { get; set; } = new ObservableCollection<BoothItem>();
        private ICollectionView ItemsView => CollectionViewSource.GetDefaultView(Items);

        private readonly DatabaseManager _dbManager = new DatabaseManager(Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "download_history.db"));
        private readonly Settings _settings = SettingsManager.Load();

        private bool _showSetupGuide = true;
        public bool ShowSetupGuide
        {
            get => _showSetupGuide;
            private set
            {
                if (_showSetupGuide != value)
                {
                    _showSetupGuide = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _nextActionHint = "Booth のライブラリ JSON を読み込んで保存先を設定しましょう。";
        public string NextActionHint
        {
            get => _nextActionHint;
            private set
            {
                if (_nextActionHint != value)
                {
                    _nextActionHint = value;
                    OnPropertyChanged();
                }
            }
        }


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
                    if (!_applyingPreset)
                    {
                        SetCustomPreset();
                    }
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
                    if (!_applyingPreset)
                    {
                        SetCustomPreset();
                    }
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
                    if (!_applyingPreset)
                    {
                        SetCustomPreset();
                    }
                }
            }
        }

        private bool _autoExtractZip;
        public bool AutoExtractZip
        {
            get => _autoExtractZip;
            set
            {
                if (_autoExtractZip != value)
                {
                    _autoExtractZip = value;
                    OnPropertyChanged();
                }
            }
        }

        private readonly ObservableCollection<string> _availableTags = new ObservableCollection<string>();
        public ObservableCollection<string> AvailableTags => _availableTags;

        private readonly ObservableCollection<string> _favoriteTags = new ObservableCollection<string>();
        public ObservableCollection<string> FavoriteTags => _favoriteTags;

        public ObservableCollection<string> FavoriteFolderNames { get; } = new ObservableCollection<string>();
        public ObservableCollection<FavoriteFolderTab> FavoriteFolderTabs { get; } = new ObservableCollection<FavoriteFolderTab>();

        public ObservableCollection<string> DownloadFolderHistory { get; } = new ObservableCollection<string>();

        public ObservableCollection<FilterPreset> FilterPresets { get; } = new ObservableCollection<FilterPreset>();

        private FilterPreset? _selectedFilterPreset;
        public FilterPreset? SelectedFilterPreset
        {
            get => _selectedFilterPreset;
            set
            {
                if (_selectedFilterPreset != value)
                {
                    _selectedFilterPreset = value;
                    OnPropertyChanged();
                    if (!_applyingPreset && value != null && !value.IsCustom)
                    {
                        ApplyFilterPreset(value);
                    }
                }
            }
        }

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
                    if (!_applyingPreset)
                    {
                        SetCustomPreset();
                    }
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
                    if (!_applyingPreset)
                    {
                        SetCustomPreset();
                    }
                }
            }
        }

        private FavoriteFolderTab? _selectedFavoriteFolderTab;
        public FavoriteFolderTab? SelectedFavoriteFolderTab
        {
            get => _selectedFavoriteFolderTab;
            set
            {
                if (value == null)
                {
                    if (_selectedFavoriteFolderTab != null)
                    {
                        _selectedFavoriteFolderTab = null;
                        OnPropertyChanged();
                        ApplyFilters();
                        if (!_applyingPreset)
                        {
                            SetCustomPreset();
                        }
                    }
                }
                else if (_selectedFavoriteFolderTab == null || _selectedFavoriteFolderTab.FilterIndex != value.FilterIndex)
                {
                    _selectedFavoriteFolderTab = value;
                    OnPropertyChanged();
                    ApplyFilters();
                    if (!_applyingPreset)
                    {
                        SetCustomPreset();
                    }
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
        private bool _applyingPreset;
        private int? _pendingFavoriteFolderFilterIndex;

        public ICommand OpenLinkCommand { get; }

        // 管理用JSONファイルのパス（例：アプリケーションディレクトリ直下）
        private readonly string manageFilePath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "booth_manage.json");

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            OpenLinkCommand = new RelayCommand(OpenLink);
            _isDownloading = false;
            Items.CollectionChanged += Items_CollectionChanged;
            FavoriteFolderNames.CollectionChanged += (_, __) => UpdateFavoriteFolderTabs();
            InitializeFilterPresets();
            _selectedFilterPreset = FilterPresets.FirstOrDefault();
            OnPropertyChanged(nameof(SelectedFilterPreset));
            // apply settings
            if (_settings.DownloadFolderHistory != null)
            {
                foreach (var path in _settings.DownloadFolderHistory.Where(p => !string.IsNullOrWhiteSpace(p)))
                {
                    if (!DownloadFolderHistory.Contains(path))
                    {
                        DownloadFolderHistory.Add(path);
                    }
                }
            }
            DownloadFolderPath = string.IsNullOrWhiteSpace(_settings.DownloadPath) ? "C:\\BoothData" : _settings.DownloadPath;
            AutoExtractZip = _settings.AutoExtractZip;
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
            UpdateFavoriteFolderTabs();
            // 起動後に管理用JSONを読み込む
            Loaded += async (_, __) => await LoadManagementDataAsync();
            UpdateGuidanceState();
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

            UpdateGuidanceState();
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
            _settings.DownloadFolderHistory = DownloadFolderHistory.ToList();
            _settings.FavoriteTags = _favoriteTags.ToList();
            _settings.FavoriteFolders = FavoriteFolderNames.ToArray();
            _settings.AutoExtractZip = AutoExtractZip;
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
                    _settings.FavoriteFolders,
                    AutoExtractZip,
                    _dbManager,
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

            UpdateGuidanceState();
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
                    UpdateDownloadFolderHistory(value);
                    UpdateDownloadStatus();
                    UpdateGuidanceState();
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

        private void UpdateDownloadFolderHistory(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            int existingIndex = DownloadFolderHistory.IndexOf(path);
            if (existingIndex > 0)
            {
                DownloadFolderHistory.RemoveAt(existingIndex);
            }
            if (existingIndex != 0)
            {
                DownloadFolderHistory.Insert(0, path);
            }

            while (DownloadFolderHistory.Count > 10)
            {
                DownloadFolderHistory.RemoveAt(DownloadFolderHistory.Count - 1);
            }

            _settings.DownloadFolderHistory = DownloadFolderHistory.ToList();
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

        private void CopyDownloadFolderPath(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(DownloadFolderPath))
                {
                    Clipboard.SetText(DownloadFolderPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"パスをコピーできませんでした: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    if (!download.IsDownloaded && AutoExtractZip && download.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        string extractDir = Path.Combine(
                            DownloadFolderPath,
                            PathUtils.Sanitize(item.ShopName),
                            PathUtils.Sanitize(item.ProductName),
                            Path.GetFileNameWithoutExtension(PathUtils.Sanitize(download.FileName)));
                        if (Directory.Exists(extractDir))
                        {
                            download.IsDownloaded = Directory.EnumerateFileSystemEntries(extractDir).Any();
                        }
                    }
                }
                item.IsDownloaded = item.Downloads.All(d => d.IsDownloaded);

                var assignedFolders = item.Downloads
                    .Select(d => d.FavoriteFolderIndex >= 0 ? d.FavoriteFolderIndex : item.FavoriteFolderIndex)
                    .Where(i => i >= 0 && i < _settings.FavoriteFolders.Length)
                    .Distinct()
                    .ToList();

                if (assignedFolders.Count == 1)
                {
                    int idx = assignedFolders[0];
                    string favRoot = _settings.FavoriteFolders[idx];
                    bool allCopied = true;
                    foreach (var d in item.Downloads)
                    {
                        int dIdx = d.FavoriteFolderIndex >= 0 ? d.FavoriteFolderIndex : item.FavoriteFolderIndex;
                        if (dIdx != idx)
                        {
                            allCopied = false;
                            break;
                        }
                        string p = Path.Combine(
                            favRoot,
                            PathUtils.Sanitize(item.ShopName),
                            PathUtils.Sanitize(item.ProductName),
                            PathUtils.Sanitize(d.FileName));
                        bool exists = File.Exists(p);
                        if (!exists && AutoExtractZip && d.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                        {
                            string extractDir = Path.Combine(
                                favRoot,
                                PathUtils.Sanitize(item.ShopName),
                                PathUtils.Sanitize(item.ProductName),
                                Path.GetFileNameWithoutExtension(PathUtils.Sanitize(d.FileName)));
                            if (Directory.Exists(extractDir))
                            {
                                exists = Directory.EnumerateFileSystemEntries(extractDir).Any();
                            }
                        }
                        if (!exists)
                        {
                            allCopied = false;
                            break;
                        }
                    }
                    if (allCopied)
                    {
                        item.CopiedFavoriteFolderIndex = idx;
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
            UpdateFavoriteFolderTabs();
            ApplyFilters();
        }

        private void UpdateFavoriteFolderTabs()
        {
            int desiredIndex = _pendingFavoriteFolderFilterIndex ?? _selectedFavoriteFolderTab?.FilterIndex ?? -1;

            FavoriteFolderTabs.Clear();
            FavoriteFolderTabs.Add(new FavoriteFolderTab("All", -1));
            FavoriteFolderTabs.Add(new FavoriteFolderTab("未選択", -2));

            for (int i = 0; i < FavoriteFolderNames.Count; i++)
            {
                if (Items.Any(item => item.FavoriteFolderIndex == i))
                {
                    FavoriteFolderTabs.Add(new FavoriteFolderTab(FavoriteFolderNames[i], i));
                }
            }

            FavoriteFolderTab? target = FavoriteFolderTabs.FirstOrDefault(t => t.FilterIndex == desiredIndex)
                                       ?? FavoriteFolderTabs.FirstOrDefault();

            if (target != null)
            {
                _pendingFavoriteFolderFilterIndex = null;
                if (_selectedFavoriteFolderTab == null || _selectedFavoriteFolderTab.FilterIndex != target.FilterIndex)
                {
                    _applyingPreset = true;
                    SelectedFavoriteFolderTab = target;
                    _applyingPreset = false;
                }
            }
            else
            {
                _selectedFavoriteFolderTab = null;
                OnPropertyChanged(nameof(SelectedFavoriteFolderTab));
            }
        }

        private void InitializeFilterPresets()
        {
            FilterPresets.Clear();
            FilterPresets.Add(FilterPreset.CreateCustom());
            FilterPresets.Add(FilterPreset.Create(
                "未DL + 更新あり",
                showOnlyNotDownloaded: true,
                showOnlyUpdates: true,
                showOnlyFavorites: false,
                tag: "All",
                favoriteFolderFilterIndex: -1,
                resetSearch: true));
            FilterPresets.Add(FilterPreset.Create(
                "お気に入りタグ + 未DL",
                showOnlyNotDownloaded: true,
                showOnlyUpdates: false,
                showOnlyFavorites: true,
                tag: "All",
                favoriteFolderFilterIndex: -1,
                resetSearch: true));
        }

        private void ApplyFilterPreset(FilterPreset preset)
        {
            _applyingPreset = true;
            try
            {
                if (preset.ShowOnlyNotDownloaded.HasValue)
                {
                    ShowOnlyNotDownloaded = preset.ShowOnlyNotDownloaded.Value;
                }
                if (preset.ShowOnlyUpdates.HasValue)
                {
                    ShowOnlyUpdates = preset.ShowOnlyUpdates.Value;
                }
                if (preset.ShowOnlyFavorites.HasValue)
                {
                    ShowOnlyFavorites = preset.ShowOnlyFavorites.Value;
                }
                if (preset.Tag != null)
                {
                    SelectedTag = preset.Tag;
                }
                if (preset.FavoriteFolderFilterIndex.HasValue)
                {
                    SetFavoriteFolderFilter(preset.FavoriteFolderFilterIndex.Value);
                }
                if (preset.ResetSearch)
                {
                    SearchQuery = string.Empty;
                }
            }
            finally
            {
                _applyingPreset = false;
            }
            ApplyFilters();
        }

        private void SetCustomPreset()
        {
            if (FilterPresets.Count == 0)
            {
                return;
            }

            var custom = FilterPresets.First();
            if (!ReferenceEquals(_selectedFilterPreset, custom))
            {
                _selectedFilterPreset = custom;
                OnPropertyChanged(nameof(SelectedFilterPreset));
            }
        }

        private void SetFavoriteFolderFilter(int filterIndex)
        {
            var tab = FavoriteFolderTabs.FirstOrDefault(t => t.FilterIndex == filterIndex);
            if (tab != null)
            {
                SelectedFavoriteFolderTab = tab;
            }
            else
            {
                _pendingFavoriteFolderFilterIndex = filterIndex;
            }
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
                        await Dispatcher.InvokeAsync(() =>
                        {
                            item.Tags = fetched.Tags;
                            item.TagsFetched = true;
                        });
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

            int filterIndex = SelectedFavoriteFolderTab?.FilterIndex ?? -1;

            ItemsView.Filter = obj =>
            {
                if (obj is not BoothItem item) return false;
                return FilterManager.Matches(item, ShowOnlyNotDownloaded, SelectedTag, ShowOnlyUpdates, SearchQuery, ShowOnlyFavorites, FavoriteTags, filterIndex);
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
            }
            UpdateDownloadStatus();
            UpdateGuidanceState();
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

        private void OpenFavoriteFolderAssign(object sender, RoutedEventArgs e)
        {
            if (itemsListView.SelectedItem is BoothItem item)
            {
                var window = new FavoriteFolderAssignWindow(item, FavoriteFolderNames.ToList());
                if (window.ShowDialog() == true)
                {
                    SaveManagementData();
                    UpdateDownloadStatus();
                    UpdateGuidanceState();
                }
            }
            else
            {
                MessageBox.Show("アイテムを選択してください。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // Favorite folder assignment and removal are handled from
        // FavoriteFolderAssignWindow only.

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

        private void Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateGuidanceState();
            UpdateFavoriteFolderTabs();
        }

        private void UpdateGuidanceState()
        {
            bool hasItems = Items.Count > 0;
            ShowSetupGuide = !hasItems;

            if (!hasItems)
            {
                NextActionHint = "① JSON を読み込み、② 保存先を指定してダウンロードの準備をしましょう。";
            }
            else if (!Directory.Exists(DownloadFolderPath))
            {
                NextActionHint = "保存先フォルダーが存在しません。📂選択 ボタンから保存先を設定してください。";
            }
            else
            {
                NextActionHint = "ダウンロードしたいアイテムをチェックし、⬇️ ダウンロード開始 を押してください。";
            }
        }

        public class FavoriteFolderTab
        {
            public FavoriteFolderTab(string header, int filterIndex)
            {
                Header = header;
                FilterIndex = filterIndex;
            }

            public string Header { get; }
            public int FilterIndex { get; }
        }

        public class FilterPreset
        {
            private FilterPreset(string name, bool isCustom, bool? showOnlyNotDownloaded, bool? showOnlyUpdates, bool? showOnlyFavorites, string? tag, int? favoriteFolderFilterIndex, bool resetSearch)
            {
                Name = name;
                IsCustom = isCustom;
                ShowOnlyNotDownloaded = showOnlyNotDownloaded;
                ShowOnlyUpdates = showOnlyUpdates;
                ShowOnlyFavorites = showOnlyFavorites;
                Tag = tag;
                FavoriteFolderFilterIndex = favoriteFolderFilterIndex;
                ResetSearch = resetSearch;
            }

            public string Name { get; }
            public bool IsCustom { get; }
            public bool? ShowOnlyNotDownloaded { get; }
            public bool? ShowOnlyUpdates { get; }
            public bool? ShowOnlyFavorites { get; }
            public string? Tag { get; }
            public int? FavoriteFolderFilterIndex { get; }
            public bool ResetSearch { get; }

            public static FilterPreset CreateCustom() => new FilterPreset("カスタム", true, null, null, null, null, null, false);

            public static FilterPreset Create(string name, bool? showOnlyNotDownloaded = null, bool? showOnlyUpdates = null, bool? showOnlyFavorites = null, string? tag = null, int? favoriteFolderFilterIndex = null, bool resetSearch = false)
            {
                return new FilterPreset(name, false, showOnlyNotDownloaded, showOnlyUpdates, showOnlyFavorites, tag, favoriteFolderFilterIndex, resetSearch);
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
