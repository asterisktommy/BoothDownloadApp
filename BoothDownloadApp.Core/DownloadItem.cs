using System;
namespace BoothDownloadApp
{
    public class DownloadItem
    {
        public string Name { get; set; }
        public string URL { get; set; }  // これを追加

        public DownloadItem(string name, string url)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            URL = url ?? throw new ArgumentNullException(nameof(url));
        }
    }
}
