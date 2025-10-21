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
                string sanitizedName = PathUtils.Sanitize(downloadName);
                string downloadedPath = Path.Combine(downloadsFolder, downloadName);
                string destFolder = Path.Combine(
                    rootPath,
                    PathUtils.Sanitize(entry.item.ShopName),
                    PathUtils.Sanitize(entry.item.ProductName));
                Directory.CreateDirectory(destFolder);
                string destPath = Path.Combine(destFolder, sanitizedName);
                bool isZip = sanitizedName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);

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
                        string favDest = Path.Combine(favFolder, sanitizedName);
                        try
                        {
                            File.Copy(destPath, favDest, true);
                            if (autoExtractZip && isZip)
                            {
                                string favExtractDir = Path.Combine(
                                    favFolder,
                                    Path.GetFileNameWithoutExtension(sanitizedName));
                                ExtractZipSafely(favDest, favExtractDir);
                            }
                        }
                        catch { }
                    }
                }

                if (autoExtractZip && isZip)
                {
                    string extractDir = Path.Combine(destFolder, Path.GetFileNameWithoutExtension(sanitizedName));
                    ExtractZipSafely(destPath, extractDir);
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

        private static void ExtractZipSafely(string zipPath, string extractDir)
        {
            try
            {
                if (!File.Exists(zipPath))
                {
                    return;
                }

                Directory.CreateDirectory(extractDir);
                string extractRoot = Path.GetFullPath(extractDir);
                using var archive = ZipFile.OpenRead(zipPath);
                foreach (var zipEntry in archive.Entries)
                {
                    string destinationPath = Path.GetFullPath(Path.Combine(extractDir, zipEntry.FullName));
                    if (!destinationPath.StartsWith(extractRoot, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(zipEntry.Name))
                    {
                        Directory.CreateDirectory(destinationPath);
                    }
                    else
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                        zipEntry.ExtractToFile(destinationPath, true);
                    }
                }

                File.Delete(zipPath);
            }
            catch
            {
                // ignore extraction errors to avoid interrupting the download workflow
            }
        }
    }
}

