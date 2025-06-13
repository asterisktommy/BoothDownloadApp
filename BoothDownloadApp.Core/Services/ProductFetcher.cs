using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BoothDownloadApp
{
    public static class ProductFetcher
    {
        private static readonly HttpClient Client = new HttpClient();

        public static async Task<BoothItem?> FetchItemAsync(string url)
        {
            var m = Regex.Match(url, @"items/(\d+)");
            if (!m.Success) return null;
            string id = m.Groups[1].Value;
            string api = $"https://booth.pm/ja/items/{id}.json";
            try
            {
                string json = await Client.GetStringAsync(api);
                using JsonDocument doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var item = new BoothItem
                {
                    ProductName = root.GetProperty("name").GetString() ?? string.Empty,
                    ShopName = root.GetProperty("shop").GetProperty("name").GetString() ?? string.Empty,
                    Thumbnail = root.GetProperty("images")[0].GetProperty("resized").GetString() ?? string.Empty,
                    ItemUrl = root.GetProperty("url").GetString() ?? url,
                    ShopUrl = root.GetProperty("shop").GetProperty("url").GetString() ?? string.Empty
                };
                if (root.TryGetProperty("tags", out var tags))
                {
                    foreach (var t in tags.EnumerateArray())
                    {
                        if (t.TryGetProperty("name", out var tn))
                        {
                            var name = tn.GetString();
                            if (!string.IsNullOrWhiteSpace(name))
                                item.Tags.Add(name);
                        }
                    }
                }
                if (root.TryGetProperty("variations", out var vars))
                {
                    foreach (var v in vars.EnumerateArray())
                    {
                        if (v.TryGetProperty("downloadable", out var dl) && dl.ValueKind != JsonValueKind.Null)
                        {
                            if (dl.TryGetProperty("no_musics", out var files))
                                AddDownloads(item, files);
                            if (dl.TryGetProperty("musics", out var musics))
                                AddDownloads(item, musics);
                        }
                    }
                }
                return item;
            }
            catch
            {
                return null;
            }
        }

        private static void AddDownloads(BoothItem item, JsonElement list)
        {
            foreach (var f in list.EnumerateArray())
            {
                string fileName = f.GetProperty("name").GetString() ?? string.Empty;
                string link = f.GetProperty("url").GetString() ?? string.Empty;
                item.Downloads.Add(new BoothItem.DownloadInfo { FileName = fileName, DownloadLink = link });
            }
        }
    }
}
