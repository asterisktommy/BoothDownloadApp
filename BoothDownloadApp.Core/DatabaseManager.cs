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

        public void SaveHistoryItem(string name, string url)
        {
            using var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;");
            connection.Open();
            using var command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS History (Id INTEGER PRIMARY KEY, Name TEXT, URL TEXT);", connection);
            command.ExecuteNonQuery();
            using var insert = new SQLiteCommand("INSERT INTO History (Name, URL) VALUES (@name, @url);", connection);
            insert.Parameters.AddWithValue("@name", name);
            insert.Parameters.AddWithValue("@url", url);
            insert.ExecuteNonQuery();
        }

        public List<DownloadItem> LoadHistory()
        {
            var history = new List<DownloadItem>();
            using var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;");
            connection.Open();
            using var command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS History (Id INTEGER PRIMARY KEY, Name TEXT, URL TEXT);", connection);
            command.ExecuteNonQuery();

            using var select = new SQLiteCommand("SELECT Name, URL FROM History ORDER BY Id DESC;", connection);
            using SQLiteDataReader reader = select.ExecuteReader();
            while (reader.Read())
            {
                history.Add(new DownloadItem(reader.GetString(0), reader.GetString(1)));
            }
            return history;
        }
    }
}
