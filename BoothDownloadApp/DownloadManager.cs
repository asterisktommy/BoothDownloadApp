using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace BoothDownloadApp
{
    public class DownloadManager
    {
        // フィールド名は _items として重複を回避
        private readonly ObservableCollection<DownloadItem> _items;
        private readonly Action<int> _updateProgress;
        private bool _isDownloading = false;

        public DownloadManager(ObservableCollection<DownloadItem> items, Action<int> updateProgress)
        {
            _items = items;
            _updateProgress = updateProgress;
        }

        public async Task StartDownloadAsync()
        {
            if (_isDownloading)
                return;

            _isDownloading = true;
            // サンプルとして、0～100 の進捗を更新
            for (int i = 0; i <= 100 && _isDownloading; i++)
            {
                await Task.Delay(50);
                _updateProgress(i);
            }
            _isDownloading = false;
        }

        public void StopDownload()
        {
            _isDownloading = false;
        }
    }
}
