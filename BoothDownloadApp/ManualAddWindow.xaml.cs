using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace BoothDownloadApp
{
    public partial class ManualAddWindow : Window
    {
        private readonly List<string> _files = new();
        private CancellationTokenSource? _fetchCts;
        private string _lastFetchedUrl = string.Empty;
        public BoothItem? ResultItem { get; private set; }
        public IReadOnlyList<string> SelectedFilePaths => _files;

        public ManualAddWindow()
        {
            InitializeComponent();
        }

        private async void FetchInfo_Click(object sender, RoutedEventArgs e)
        {
            await FetchInfoAsync(UrlTextBox.Text.Trim());
        }

        private async Task FetchInfoAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            try
            {
                var details = await FetchItemDetails(url);
                if (details != null)
                {
                    ProductNameTextBox.Text = details.ProductName;
                    ShopNameTextBox.Text = details.ShopName;
                    TagsTextBox.Text = string.Join(", ", details.Tags);
                }
                else
                {
                    MessageBox.Show("情報を取得できませんでした。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"情報取得に失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void UrlTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            _fetchCts?.Cancel();
            var url = UrlTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(url))
            {
                _lastFetchedUrl = string.Empty;
                return;
            }
            _fetchCts = new CancellationTokenSource();
            var token = _fetchCts.Token;
            try
            {
                await Task.Delay(800, token);
                if (token.IsCancellationRequested) return;
                if (url != _lastFetchedUrl)
                {
                    await FetchInfoAsync(url);
                    _lastFetchedUrl = url;
                }
            }
            catch (TaskCanceledException) { }
        }

        private static async Task<ItemDetails?> FetchItemDetails(string url)
        {
            var m = Regex.Match(url, @"items/(\d+)");
            if (!m.Success) return null;
            string id = m.Groups[1].Value;
            var uri = new Uri(url);
            string api = $"{uri.Scheme}://{uri.Host}/items/{id}.json";
            using HttpClient client = new HttpClient();
            using var stream = await client.GetStreamAsync(api);
            using var doc = await JsonDocument.ParseAsync(stream);
            var root = doc.RootElement;
            string product = root.GetProperty("name").GetString() ?? string.Empty;
            string shop = root.GetProperty("shop").GetProperty("name").GetString() ?? string.Empty;
            List<string> tags = new();
            if (root.TryGetProperty("tags", out var tagElem))
            {
                foreach (var t in tagElem.EnumerateArray())
                {
                    if (t.TryGetProperty("name", out var n))
                    {
                        tags.Add(n.GetString() ?? string.Empty);
                    }
                }
            }
            return new ItemDetails(product, shop, tags);
        }

        private record ItemDetails(string ProductName, string ShopName, List<string> Tags);

        private void AddFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Multiselect = true };
            if (dialog.ShowDialog() == true)
            {
                foreach (var f in dialog.FileNames)
                {
                    _files.Add(f);
                    FilesListBox.Items.Add(System.IO.Path.GetFileName(f));
                }
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ProductNameTextBox.Text) || string.IsNullOrWhiteSpace(ShopNameTextBox.Text))
            {
                MessageBox.Show("商品名とショップ名を入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ResultItem = new BoothItem
            {
                ProductName = ProductNameTextBox.Text,
                ShopName = ShopNameTextBox.Text,
                Tags = TagsTextBox.Text.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList(),
                Downloads = _files.Select(f => new BoothItem.DownloadInfo
                {
                    FileName = System.IO.Path.GetFileName(f),
                    DownloadLink = string.Empty,
                    IsDownloaded = true
                }).ToList(),
                IsDownloaded = true
            };

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _fetchCts?.Cancel();
            base.OnClosing(e);
        }
    }
}
