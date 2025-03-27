using System.Collections.Generic;
using System.Linq;
using BoothDownloadApp;

public class FilterManager
{
    public List<DownloadItem> ApplyFilters(List<DownloadItem> items, string filter)
    {
        return items.Where(item => item.Name.Contains(filter)).ToList();
    }
}
