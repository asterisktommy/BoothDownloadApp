using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;

namespace BoothDownloadApp
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
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
            StartWebServer();
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
                string defaultJson = JsonSerializer.Serialize(emptyLibrary, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(manageFilePath, defaultJson);
            }

            try
            {
                string json = File.ReadAllText(manageFilePath);
                var boothLibrary = JsonSerializer.Deserialize<BoothLibrary>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
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
            string json = JsonSerializer.Serialize(boothLibrary, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(manageFilePath, json);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            SaveManagementData();
            base.OnClosing(e);
        }

        private void StartWebServer()
        {
            var builder = WebApplication.CreateBuilder();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });
            var app = builder.Build();
            app.UseCors("AllowAll");

            app.MapPost("/api/booth", async (HttpContext context) =>
            {
                using var reader = new StreamReader(context.Request.Body);
                var json = await reader.ReadToEndAsync();

                try
                {
                    var boothLibrary = JsonSerializer.Deserialize<BoothLibrary>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (boothLibrary?.Library != null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Items.Clear();
                            foreach (var item in boothLibrary.Library)
                            {
                                Items.Add(item);
                            }
                        });
                    }
                    return Results.Json(new { message = "データ受信成功" });
                }
                catch (JsonException ex)
                {
                    return Results.Json(new { error = $"JSON パースエラー: {ex.Message}" }, statusCode: 400);
                }
            });

            Task.Run(() => app.Run("http://localhost:5000"));
        }

        private async void StartDownload(object sender, RoutedEventArgs e)
        {
            if (Items.Count == 0)
            {
                MessageBox.Show("ダウンロードするアイテムがありません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            MessageBox.Show("ダウンロード開始！（機能未実装）", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
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
                    var boothLibrary = JsonSerializer.Deserialize<BoothLibrary>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

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
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        private void ListView_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // 必要に応じて選択変更時の処理を追加
        }
    }

    public class BoothLibrary
    {
        [JsonPropertyName("library")]
        public List<BoothItem> Library { get; set; }
    }

    public class BoothItem : INotifyPropertyChanged
    {
        private bool _isSelected;

        [JsonPropertyName("productName")]
        public string ProductName { get; set; }

        [JsonPropertyName("shopName")]
        public string ShopName { get; set; }

        [JsonPropertyName("thumbnail")]
        public string Thumbnail { get; set; }

        [JsonPropertyName("downloads")]
        public List<DownloadInfo> Downloads { get; set; }

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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // 内部クラス DownloadInfo
        public class DownloadInfo : INotifyPropertyChanged
        {
            private bool _isSelected;
            private string _description;

            [JsonPropertyName("fileName")]
            public string FileName { get; set; }

            [JsonPropertyName("downloadLink")]
            public string DownloadLink { get; set; }

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

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
