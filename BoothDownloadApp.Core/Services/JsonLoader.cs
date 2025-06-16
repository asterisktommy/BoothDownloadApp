using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace BoothDownloadApp
{
    public static class JsonLoader
    {
        public static List<BoothItem>? LoadItems(string json, JsonSerializerOptions options)
        {
            try
            {
                var library = JsonSerializer.Deserialize<BoothLibrary>(json, options);
                if (library != null)
                {
                    var list = new List<BoothItem>();
                    if (library.Library != null) list.AddRange(library.Library);
                    if (library.Gifts != null) list.AddRange(library.Gifts);
                    if (list.Count > 0)
                    {
                        foreach (var item in list)
                        {
                            item.TagsFetched = item.Tags != null && item.Tags.Count > 0;
                        }
                        return list;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to parse BoothLibrary JSON: {ex.Message}");
            }
            try
            {
                var simple = JsonSerializer.Deserialize<List<SimpleBoothItem>>(json, options);
                if (simple != null)
                {
                    return simple.Select(si => new BoothItem
                    {
                        ProductName = si.Title,
                        ShopName = si.ShopName,
                        Thumbnail = si.ImageUrl,
                        ItemUrl = si.ItemUrl,
                        ShopUrl = si.ShopUrl,
                        Downloads = si.Files.Select(f => new BoothItem.DownloadInfo
                        {
                            FileName = f.FileName,
                            DownloadLink = f.DownloadUrl
                        }).ToList(),
                        Tags = si.Tags,
                        TagsFetched = si.Tags != null && si.Tags.Count > 0
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to parse SimpleBoothItem JSON: {ex.Message}");
            }
            return null;
        }
    }
}
