using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;

namespace BoothDownloadApp
{
    public static class SettingsManager
    {
        private static readonly string SettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { WriteIndented = true };

        public static Settings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    var settings = JsonSerializer.Deserialize<Settings>(json, JsonOptions);
                    if (settings != null)
                    {
                        if (settings.FavoriteFolders == null || settings.FavoriteFolders.Length != 10)
                        {
                            settings.FavoriteFolders = DefaultFolders();
                        }
                        return settings;
                    }
                }
            }
            catch
            {
                // ignore and fall back to defaults
            }
            return new Settings {
                DownloadPath = "C:\\BoothData",
                RetryCount = 3,
                FavoriteTags = new List<string>(),
                FavoriteFolders = DefaultFolders()
            };
        }

        public static void Save(Settings settings)
        {
            try
            {
                string json = JsonSerializer.Serialize(settings, JsonOptions);
                File.WriteAllText(SettingsPath, json);
            }
            catch
            {
                // ignore save errors
            }
        }

        private static string[] DefaultFolders()
        {
            var arr = new string[10];
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = $"Favorite{i + 1}";
            }
            return arr;
        }
    }
}
