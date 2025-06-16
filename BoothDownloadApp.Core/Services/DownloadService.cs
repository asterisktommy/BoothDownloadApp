using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BoothDownloadApp
{
    /// <summary>
    /// Provides shared logic for downloading Booth item files.
    /// </summary>
    public static class DownloadService
    {
        public static async Task DownloadItemsAsync(IEnumerable<BoothItem> items, Func<BoothItem.DownloadInfo, bool> fileSelector, string rootPath, int retryCount, DatabaseManager db, string[] favoriteFolders, bool autoExtractZip, IProgress<int>? progress, CancellationToken token)
        {
            using HttpClient httpClient = new HttpClient();
            var fileList = items.SelectMany(i => i.Downloads.Where(fileSelector).Select(d => (item: i, file: d))).ToList();
            int totalFiles = fileList.Count;
            int downloaded = 0;

            foreach (var entry in fileList)
            {
                token.ThrowIfCancellationRequested();
                string folder = Path.Combine(rootPath,
                    PathUtils.Sanitize(entry.item.ShopName),
                    PathUtils.Sanitize(entry.item.ProductName));
                Directory.CreateDirectory(folder);
                string path = Path.Combine(folder, PathUtils.Sanitize(entry.file.FileName));
                int attempts = 0;
                while (true)
                {
                    try
                    {
                        using HttpResponseMessage response = await httpClient.GetAsync(entry.file.DownloadLink, token);
                        response.EnsureSuccessStatusCode();
                        await using FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
                        await response.Content.CopyToAsync(fs, token);

                        entry.file.IsDownloaded = true;
                        entry.file.IsSelected = false;
                        downloaded++;
                        progress?.Report((int)((double)downloaded / totalFiles * 100));
                        db.SaveHistoryItem(PathUtils.Sanitize(entry.file.FileName), entry.file.DownloadLink);

                        if (entry.item.FavoriteFolderIndex >= 0 && entry.item.FavoriteFolderIndex < favoriteFolders.Length)
                        {
                            string favRoot = favoriteFolders[entry.item.FavoriteFolderIndex];
                            if (!string.IsNullOrWhiteSpace(favRoot))
                            {
                                string favFolder = Path.Combine(favRoot,
                                    PathUtils.Sanitize(entry.item.ShopName),
                                    PathUtils.Sanitize(entry.item.ProductName));
                                Directory.CreateDirectory(favFolder);
                                string dest = Path.Combine(favFolder, PathUtils.Sanitize(entry.file.FileName));
                                try
                                {
                                    File.Copy(path, dest, true);
                                    if (autoExtractZip && dest.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                                    {
                                        string extractDir = Path.Combine(Path.GetDirectoryName(dest)!, Path.GetFileNameWithoutExtension(dest));
                                        Directory.CreateDirectory(extractDir);
                                        try
                                        {
                                            using ZipArchive archive = ZipFile.OpenRead(dest);
                                            string extractRoot = Path.GetFullPath(extractDir);
                                            foreach (var entry in archive.Entries)
                                            {
                                                string entryDest = Path.GetFullPath(Path.Combine(extractDir, entry.FullName));
                                                if (!entryDest.StartsWith(extractRoot, StringComparison.Ordinal))
                                                {
                                                    continue;
                                                }
                                                if (string.IsNullOrEmpty(entry.Name))
                                                {
                                                    Directory.CreateDirectory(entryDest);
                                                }
                                                else
                                                {
                                                    Directory.CreateDirectory(Path.GetDirectoryName(entryDest)!);
                                                    entry.ExtractToFile(entryDest, true);
                                                }
                                            }
                                            File.Delete(dest);
                                        }
                                        catch { }
                                    }
                                }
                                catch { }
                            }
                        }
                        break;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch
                    {
                        attempts++;
                        if (attempts > retryCount)
                        {
                            break;
                        }
                        await Task.Delay(1000, token);
                    }
                }
            }

            foreach (var i in items)
            {
                i.IsDownloaded = i.Downloads.All(d => d.IsDownloaded);
                i.IsSelected = false;
            }
        }
    }
}

