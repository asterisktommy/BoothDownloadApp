namespace BoothDownloadApp
{
    using System.Data.SQLite;
    using System.IO;
    using System.Collections.Generic;

    public class DatabaseManager
    {
        private string _dbPath;

        public DatabaseManager(string dbPath)
        {
            this._dbPath = dbPath;
            if (!File.Exists(dbPath))
            {
                SQLiteConnection.CreateFile(dbPath);
            }
        }

        public void SaveHistory(List<DownloadItem> items)
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();
                var command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS History (Id INTEGER PRIMARY KEY, Name TEXT, URL TEXT);", connection);
                command.ExecuteNonQuery();

                foreach (var item in items)
                {
                    command = new SQLiteCommand($"INSERT INTO History (Name, URL) VALUES ('{item.Name}', '{item.URL}');", connection);
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
