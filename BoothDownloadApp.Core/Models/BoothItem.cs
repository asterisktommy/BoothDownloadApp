using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace BoothDownloadApp.Core;

public class BoothItem : INotifyPropertyChanged
{
    private bool _isSelected;
    private bool _isDownloaded;

    [JsonPropertyName("productName")]
    public string ProductName { get; set; } = string.Empty;

    [JsonPropertyName("shopName")]
    public string ShopName { get; set; } = string.Empty;

    [JsonPropertyName("thumbnail")]
    public string Thumbnail { get; set; } = string.Empty;

    [JsonPropertyName("downloads")]
    public List<DownloadInfo> Downloads { get; set; } = new();

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
                foreach (var d in Downloads)
                    d.IsSelected = value;
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
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public class DownloadInfo : INotifyPropertyChanged
    {
        private bool _isSelected;
        private bool _isDownloaded;

        [JsonPropertyName("fileName")]
        public string FileName { get; set; } = string.Empty;

        [JsonPropertyName("downloadLink")]
        public string DownloadLink { get; set; } = string.Empty;

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
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
