using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace BoothDownloadApp.Core;

public static class BoothDataService
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public static async Task<List<BoothItem>> LoadAsync(string path)
    {
        if (!File.Exists(path))
            return new List<BoothItem>();
        await using var fs = File.OpenRead(path);
        var lib = await JsonSerializer.DeserializeAsync<BoothLibrary>(fs, Options);
        return lib?.Library ?? new List<BoothItem>();
    }

    public static async Task SaveAsync(string path, IEnumerable<BoothItem> items)
    {
        var lib = new BoothLibrary { Library = items.ToList() };
        await using var fs = File.Create(path);
        await JsonSerializer.SerializeAsync(fs, lib, Options);
    }
}
