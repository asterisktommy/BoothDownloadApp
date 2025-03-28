namespace BoothDownloadApp
{
    public class Settings
    {
        public bool AutoLogin { get; set; }
        public bool DarkMode { get; set; }
        public string DownloadPath { get; set; } = string.Empty;
        public int RetryCount { get; set; }
    }
}
