using System;
using System.Collections.Generic;
namespace BoothDownloadApp
{
    public class DownloadItem
    {
        public string Name { get; set; }
        public string URL { get; set; }  // これを追加
        public List<string> Tags { get; set; } = new List<string>();

        public DownloadItem(string name, string url)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            URL = url ?? throw new ArgumentNullException(nameof(url));
        }
    }
}
