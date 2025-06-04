using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using BoothDownloadApp.Core;

namespace BoothDownloadApp.Maui;

public partial class MainPage : ContentPage
{
    private readonly string manageFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "booth_manage.json");

    public ObservableCollection<BoothItem> Items { get; } = new();

    public ICommand LoadCommand { get; }
    public ICommand DownloadCommand { get; }

    public MainPage()
    {
        InitializeComponent();
        BindingContext = this;
        LoadCommand = new Command(async () => await LoadAsync());
        DownloadCommand = new Command(async () => await DownloadAsync());
    }

    private async Task LoadAsync()
    {
        var items = await BoothDataService.LoadAsync(manageFilePath);
        Items.Clear();
        foreach (var item in items)
            Items.Add(item);
    }

    private async Task DownloadAsync()
    {
        string folder = Path.Combine(FileSystem.AppDataDirectory, "Downloads");
        foreach (var item in Items.Where(i => i.IsSelected || i.Downloads.Any(d => d.IsSelected)))
        {
            await Downloader.DownloadAsync(item, folder);
        }
    }
}

