namespace BoothDownloadApp
{
    using System.Data.SQLite;
    using System.IO;
    using System.Collections.Generic;

    public class DownloadManager
    {
        private string dbPath;
        private bool _isDownloading; // Remove explicit initialization

        public DownloadManager(string dbPath)
        {
            this.dbPath = dbPath;
            if (!File.Exists(dbPath))
            {
                SQLiteConnection.CreateFile(dbPath);
            }
            _isDownloading = false; // Initialize in constructor
        }

        public void SaveHistory(List<DownloadItem> items)
        {
            using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                connection.Open();
                using var command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS History (Id INTEGER PRIMARY KEY, Name TEXT, URL TEXT);", connection);
                command.ExecuteNonQuery();

                foreach (var item in items)
                {
                    using var insert = new SQLiteCommand("INSERT INTO History (Name, URL) VALUES (@name, @url);", connection);
                    insert.Parameters.AddWithValue("@name", item.Name);
                    insert.Parameters.AddWithValue("@url", item.URL);
                    insert.ExecuteNonQuery();
                }
            }
        }
    }
}
