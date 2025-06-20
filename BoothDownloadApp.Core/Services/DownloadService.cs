using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace BoothDownloadApp
{
    /// <summary>
    /// Provides shared logic for downloading Booth item files.
    /// </summary>
    public static class DownloadService
    {
        public static async Task DownloadItemsAsync(
            IEnumerable<BoothItem> items,
            Func<BoothItem.DownloadInfo, bool> fileSelector,
            string rootPath,
            string[] favoriteFolders,
            bool autoExtractZip,
            DatabaseManager db,
            IProgress<int>? progress,
            CancellationToken token)
        {
            var fileList = items
                .SelectMany(i => i.Downloads.Where(fileSelector).Select(d => (item: i, file: d)))
                .ToList();
            int totalFiles = fileList.Count;
            int processed = 0;
            string downloadsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

            foreach (var entry in fileList)
            {
                token.ThrowIfCancellationRequested();

                try
                {
                    Process.Start(new ProcessStartInfo(entry.file.DownloadLink) { UseShellExecute = true });
                }
                catch { }

                string downloadName = entry.file.FileName;
                string downloadedPath = Path.Combine(downloadsFolder, downloadName);
                string destFolder = Path.Combine(
                    rootPath,
                    PathUtils.Sanitize(entry.item.ShopName),
                    PathUtils.Sanitize(entry.item.ProductName));
                Directory.CreateDirectory(destFolder);
                string destPath = Path.Combine(destFolder, PathUtils.Sanitize(downloadName));

                for (int i = 0; i < 60; i++)
                {
                    token.ThrowIfCancellationRequested();
                    if (File.Exists(downloadedPath))
                    {
                        try
                        {
                            File.Move(downloadedPath, destPath, true);
                        }
                        catch
                        {
                            // move may fail if the browser still has the file locked
                            try { File.Copy(downloadedPath, destPath, true); File.Delete(downloadedPath); } catch { }
                        }
                        break;
                    }
                    await Task.Delay(1000, token);
                }

                int folderIdx = entry.file.FavoriteFolderIndex >= 0 ? entry.file.FavoriteFolderIndex : entry.item.FavoriteFolderIndex;
                if (folderIdx >= 0 && folderIdx < favoriteFolders.Length)
                {
                    string favRoot = favoriteFolders[folderIdx];
                    if (!string.IsNullOrWhiteSpace(favRoot))
                    {
                        string favFolder = Path.Combine(
                            favRoot,
                            PathUtils.Sanitize(entry.item.ShopName),
                            PathUtils.Sanitize(entry.item.ProductName));
                        Directory.CreateDirectory(favFolder);
                        string favDest = Path.Combine(favFolder, PathUtils.Sanitize(downloadName));
                        try
                        {
                            File.Copy(destPath, favDest, true);
                            if (autoExtractZip && favDest.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                            {
                                string extractDir = Path.Combine(Path.GetDirectoryName(favDest)!, Path.GetFileNameWithoutExtension(favDest));
                                Directory.CreateDirectory(extractDir);
                                try
                                {
                                    using var archive = ZipFile.OpenRead(favDest);
                                    string extractRoot = Path.GetFullPath(extractDir);
                                    foreach (var zipEntry in archive.Entries)
                                    {
                                        string entryDest = Path.GetFullPath(Path.Combine(extractDir, zipEntry.FullName));
                                        if (!entryDest.StartsWith(extractRoot, StringComparison.Ordinal))
                                        {
                                            continue;
                                        }
                                        if (string.IsNullOrEmpty(zipEntry.Name))
                                        {
                                            Directory.CreateDirectory(entryDest);
                                        }
                                        else
                                        {
                                            Directory.CreateDirectory(Path.GetDirectoryName(entryDest)!);
                                            zipEntry.ExtractToFile(entryDest, true);
                                        }
                                    }
                                    File.Delete(favDest);
                                }
                                catch { }
                            }
                        }
                        catch { }
                    }
                }

                entry.file.IsDownloaded = true;
                entry.file.IsSelected = false;
                processed++;
                progress?.Report((int)((double)processed / totalFiles * 100));
                db.SaveHistoryItem(PathUtils.Sanitize(entry.file.FileName), entry.file.DownloadLink);
            }

            foreach (var i in items)
            {
                i.IsDownloaded = i.Downloads.All(d => d.IsDownloaded);
                i.IsSelected = false;
            }
        }
    }
}

