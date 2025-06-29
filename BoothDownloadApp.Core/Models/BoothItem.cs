using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace BoothDownloadApp
{
    public class BoothItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        private string _description = string.Empty;
        private bool _isDownloaded;

        [JsonPropertyName("productName")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("shopName")]
        public string ShopName { get; set; } = string.Empty;

        [JsonPropertyName("thumbnail")]
        public string Thumbnail { get; set; } = string.Empty;

        [JsonPropertyName("itemUrl")]
        public string ItemUrl { get; set; } = string.Empty;

        [JsonPropertyName("shopUrl")]
        public string ShopUrl { get; set; } = string.Empty;

        [JsonPropertyName("downloads")]
        public List<DownloadInfo> Downloads { get; set; } = new List<DownloadInfo>();

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new List<string>();

        [JsonPropertyName("tagsFetched")]
        public bool TagsFetched { get; set; }

        [JsonPropertyName("favoriteFolderIndex")]
        public int FavoriteFolderIndex { get; set; } = -1;

        private int _copiedFavoriteFolderIndex = -1;

        [JsonIgnore]
        public int CopiedFavoriteFolderIndex
        {
            get => _copiedFavoriteFolderIndex;
            set
            {
                if (_copiedFavoriteFolderIndex != value)
                {
                    _copiedFavoriteFolderIndex = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CopiedFavoriteFolderName));
                }
            }
        }

        private string _copiedFavoriteFolderName = string.Empty;

        [JsonIgnore]
        public string CopiedFavoriteFolderName
        {
            get => _copiedFavoriteFolderName;
            set
            {
                if (_copiedFavoriteFolderName != value)
                {
                    _copiedFavoriteFolderName = value;
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

        [JsonIgnore]
        public bool IsDownloaded
        {
            get => _isDownloaded;
            set
            {
                if (_isDownloaded != value)
                {
                    _isDownloaded = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null!)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public class DownloadInfo : INotifyPropertyChanged
        {
            private bool _isSelected;
            private string _description = string.Empty;
            private bool _isDownloaded;

            [JsonPropertyName("fileName")]
            public string FileName { get; set; } = string.Empty;

            [JsonPropertyName("downloadLink")]
            public string DownloadLink { get; set; } = string.Empty;

            private int _favoriteFolderIndex = -1;
            [JsonPropertyName("favoriteFolderIndex")]
            public int FavoriteFolderIndex
            {
                get => _favoriteFolderIndex;
                set
                {
                    if (_favoriteFolderIndex != value)
                    {
                        _favoriteFolderIndex = value;
                        OnPropertyChanged();
                    }
                }
            }

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

            [JsonIgnore]
            public bool IsDownloaded
            {
                get => _isDownloaded;
                set
                {
                    if (_isDownloaded != value)
                    {
                        _isDownloaded = value;
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
