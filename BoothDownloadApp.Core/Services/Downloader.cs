using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BoothDownloadApp.Core;

public static class Downloader
{
    public static async Task DownloadAsync(BoothItem item, string baseFolder, IProgress<double>? progress = null, CancellationToken token = default)
    {
        using HttpClient client = new();
        string itemFolder = Path.Combine(baseFolder, item.ShopName, item.ProductName);
        Directory.CreateDirectory(itemFolder);

        var selected = item.Downloads.Where(d => d.IsSelected).ToList();
        int total = selected.Count;
        int count = 0;

        foreach (var file in selected)
        {
            string path = Path.Combine(itemFolder, file.FileName);
            using var response = await client.GetAsync(file.DownloadLink, token);
            response.EnsureSuccessStatusCode();
            await using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            await response.Content.CopyToAsync(fs, token);
            file.IsDownloaded = true;
            count++;
            progress?.Report((double)count / total);
        }

        item.IsDownloaded = item.Downloads.All(d => d.IsDownloaded);
    }
}
