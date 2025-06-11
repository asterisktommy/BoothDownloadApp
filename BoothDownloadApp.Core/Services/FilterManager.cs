using System.Collections.Generic;
using System.Linq;

namespace BoothDownloadApp
{
    /// <summary>
    /// Provides helper methods for filtering Booth items.
    /// </summary>
    public static class FilterManager
    {
        public static bool Matches(BoothItem item, bool showOnlyNotDownloaded, string? tag, bool showOnlyUpdates, string? search, bool showOnlyFavorites, IEnumerable<string> favoriteTags)
        {
            if (showOnlyNotDownloaded && item.IsDownloaded)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(tag) && tag != "All" && !item.Tags.Contains(tag))
            {
                return false;
            }

            if (showOnlyFavorites && !item.Tags.Any(t => favoriteTags.Contains(t)))
            {
                return false;
            }

            if (showOnlyUpdates)
            {
                bool hasNew = item.Downloads.Any(d => !d.IsDownloaded);
                bool hasOld = item.Downloads.Any(d => d.IsDownloaded);
                if (!(hasNew && hasOld))
                {
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLowerInvariant();
                bool textMatch = item.ProductName.ToLowerInvariant().Contains(search)
                                 || item.ShopName.ToLowerInvariant().Contains(search)
                                 || item.Tags.Any(t => t.ToLowerInvariant().Contains(search));
                if (!textMatch)
                {
                    return false;
                }
            }

            return true;
        }

        public static IEnumerable<BoothItem> Apply(IEnumerable<BoothItem> items, bool showOnlyNotDownloaded, string? tag, bool showOnlyUpdates, string? search, bool showOnlyFavorites, IEnumerable<string> favoriteTags)
        {
            return items.Where(i => Matches(i, showOnlyNotDownloaded, tag, showOnlyUpdates, search, showOnlyFavorites, favoriteTags));
        }
    }
}
