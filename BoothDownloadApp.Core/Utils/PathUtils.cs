using System;
using System.IO;
using System.Linq;

namespace BoothDownloadApp
{
    public static class PathUtils
    {
        /// <summary>
        /// Remove invalid file name characters from the provided string.
        /// </summary>
        public static string Sanitize(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return new string(name.Where(c => Array.IndexOf(invalid, c) == -1).ToArray());
        }
    }
}
