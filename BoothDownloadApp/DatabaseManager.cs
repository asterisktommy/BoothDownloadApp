using BoothDownloadApp;
using System.Data.SQLite;
using System.IO;

public class DatabaseManager
{
    private string dbPath;

    public DatabaseManager(string dbPath)
    {
        this.dbPath = dbPath;
        if (!File.Exists(dbPath))
        {
            SQLiteConnection.CreateFile(dbPath);
        }
    }

    public void SaveHistory(List<DownloadItem> items)
    {
        using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
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
