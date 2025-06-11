using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BoothDownloadApp
{
    public class SimpleBoothItem
    {
        [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
        [JsonPropertyName("itemUrl")] public string ItemUrl { get; set; } = string.Empty;
        [JsonPropertyName("imageUrl")] public string ImageUrl { get; set; } = string.Empty;
        [JsonPropertyName("shopName")] public string ShopName { get; set; } = string.Empty;
        [JsonPropertyName("shopUrl")] public string ShopUrl { get; set; } = string.Empty;
        [JsonPropertyName("files")] public List<SimpleFile> Files { get; set; } = new();
        [JsonPropertyName("tags")] public List<string> Tags { get; set; } = new();
    }

    public class SimpleFile
    {
        [JsonPropertyName("fileName")] public string FileName { get; set; } = string.Empty;
        [JsonPropertyName("downloadUrl")] public string DownloadUrl { get; set; } = string.Empty;
    }
}
