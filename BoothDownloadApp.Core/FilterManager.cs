namespace BoothDownloadApp
{
    public class FilterManager
    {
        public static List<DownloadItem> ApplyFilters(List<DownloadItem> items, string filter)
        {
            return items.Where(item => item.Name.Contains(filter)).ToList();
        }
    }
}
