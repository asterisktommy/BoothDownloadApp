using System;
using System.IO;
using System.IO.Compression;

namespace BoothDownloadApp
{
    public static class FavoriteFolderService
    {
        public static bool CopyFileToFavorite(string sourcePath, string destFolder, bool autoExtract)
        {
            try
            {
                Directory.CreateDirectory(destFolder);
                string destPath = Path.Combine(destFolder, Path.GetFileName(sourcePath));
                File.Copy(sourcePath, destPath, true);

                if (autoExtract && Path.GetExtension(destPath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    string extractDir = Path.Combine(destFolder, Path.GetFileNameWithoutExtension(sourcePath));
                    Directory.CreateDirectory(extractDir);
                    ZipFile.ExtractToDirectory(destPath, extractDir, true);
                    File.Delete(destPath);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
