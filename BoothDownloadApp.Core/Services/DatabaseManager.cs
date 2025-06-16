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
            _dbPath = dbPath;
            if (!File.Exists(dbPath))
            {
                SQLiteConnection.CreateFile(dbPath);
            }

            using var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;");
            connection.Open();
            using var cmd = new SQLiteCommand(connection);
            cmd.CommandText = @"CREATE TABLE IF NOT EXISTS History (Id INTEGER PRIMARY KEY, Name TEXT, URL TEXT UNIQUE);
CREATE TABLE IF NOT EXISTS Tags (Id INTEGER PRIMARY KEY, Name TEXT UNIQUE);
CREATE TABLE IF NOT EXISTS ItemTags (ItemId INTEGER, TagId INTEGER, UNIQUE(ItemId,TagId));";
            cmd.ExecuteNonQuery();
        }

        public void SaveHistory(List<DownloadItem> items)
        {
            using var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;");
            connection.Open();
            foreach (var item in items)
            {
                SaveItemWithTags(item.Name, item.URL, item.Tags, connection);
            }
        }

        private static long EnsureTag(string tag, SQLiteConnection connection)
        {
            using var insert = new SQLiteCommand("INSERT OR IGNORE INTO Tags (Name) VALUES (@name);", connection);
            insert.Parameters.AddWithValue("@name", tag);
            insert.ExecuteNonQuery();
            using var select = new SQLiteCommand("SELECT Id FROM Tags WHERE Name=@name;", connection);
            select.Parameters.AddWithValue("@name", tag);
            object? result = select.ExecuteScalar();
            return Convert.ToInt64(result ?? 0);
        }

        private static long EnsureItem(string name, string url, SQLiteConnection connection)
        {
            using var insert = new SQLiteCommand("INSERT OR IGNORE INTO History (Name, URL) VALUES (@name, @url);", connection);
            insert.Parameters.AddWithValue("@name", name);
            insert.Parameters.AddWithValue("@url", url);
            insert.ExecuteNonQuery();
            using var select = new SQLiteCommand("SELECT Id FROM History WHERE URL=@url;", connection);
            select.Parameters.AddWithValue("@url", url);
            object? result = select.ExecuteScalar();
            return Convert.ToInt64(result ?? 0);
        }

        private static void SaveItemWithTags(string name, string url, List<string> tags, SQLiteConnection connection)
        {
            var itemId = EnsureItem(name, url, connection);
            foreach (var tag in tags)
            {
                var tagId = EnsureTag(tag, connection);
                using var rel = new SQLiteCommand("INSERT OR IGNORE INTO ItemTags (ItemId, TagId) VALUES (@i,@t);", connection);
                rel.Parameters.AddWithValue("@i", itemId);
                rel.Parameters.AddWithValue("@t", tagId);
                rel.ExecuteNonQuery();
            }
        }

        public void SaveHistoryItem(string name, string url)
        {
            using var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;");
            connection.Open();
            SaveItemWithTags(name, url, new List<string>(), connection);
        }

        public List<DownloadItem> LoadHistory()
        {
            var history = new List<DownloadItem>();
            using var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;");
            connection.Open();
            using var command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS History (Id INTEGER PRIMARY KEY, Name TEXT, URL TEXT UNIQUE);", connection);
            command.ExecuteNonQuery();

            using var select = new SQLiteCommand("SELECT Id, Name, URL FROM History ORDER BY Id DESC;", connection);
            using SQLiteDataReader reader = select.ExecuteReader();
            while (reader.Read())
            {
                var id = reader.GetInt64(0);
                var name = reader.GetString(1);
                var url = reader.GetString(2);
                var tags = GetTagsForItem(id, connection);
                history.Add(new DownloadItem(name, url) { Tags = tags });
            }
            return history;
        }

        private static List<string> GetTagsForItem(long itemId, SQLiteConnection connection)
        {
            using var cmd = new SQLiteCommand("SELECT Name FROM Tags t JOIN ItemTags it ON t.Id = it.TagId WHERE it.ItemId=@id;", connection);
            cmd.Parameters.AddWithValue("@id", itemId);
            using var reader = cmd.ExecuteReader();
            var tags = new List<string>();
            while (reader.Read())
            {
                tags.Add(reader.GetString(0));
            }
            return tags;
        }

        public List<string> LoadAllTags()
        {
            var tags = new List<string>();
            using var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;");
            connection.Open();
            using var cmd = new SQLiteCommand("SELECT Name FROM Tags ORDER BY Name;", connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                tags.Add(reader.GetString(0));
            }
            return tags;
        }
    }
}
