using System.Collections.Generic;

namespace BoothDownloadApp
{
    public class Settings
    {
        public string DownloadPath { get; set; } = string.Empty;
        public int RetryCount { get; set; }
        public List<string> FavoriteTags { get; set; } = new List<string>();
        public string[] FavoriteFolders { get; set; } = new string[10];
        public bool AutoExtractZip { get; set; }
    }
}
