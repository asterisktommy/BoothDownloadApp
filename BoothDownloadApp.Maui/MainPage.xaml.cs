using System;
using System.Collections.ObjectModel;
using System.Text.Json;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using BoothDownloadApp;
using System.IO;
using System.Linq;
using System.Threading;

namespace BoothDownloadApp.Maui
{
    public partial class MainPage : ContentPage
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public ObservableCollection<BoothItem> Items { get; } = new();

        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;
        }

        private async void OnLoadJsonClicked(object sender, EventArgs e)
        {
            try
            {
                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Select booth_data.json"
                });
                if (result != null)
                {
                    using var stream = await result.OpenReadAsync();
                    using var reader = new StreamReader(stream);
                    string json = await reader.ReadToEndAsync();
                    var items = JsonLoader.LoadItems(json, JsonOptions);
                    if (items != null)
                    {
                        Items.Clear();
                        foreach (var item in items)
                        {
                            Items.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private async void OnStartDownloadClicked(object sender, EventArgs e)
        {
            var selected = Items.Where(i => i.IsSelected || i.Downloads.Any(d => d.IsSelected)).ToList();
            if (selected.Count == 0)
            {
                await DisplayAlert("Info", "No items selected", "OK");
                return;
            }
            string root = FileSystem.Current.AppDataDirectory;
            var db = new DatabaseManager(Path.Combine(root, "download_history.db"));
            try
            {
                await DownloadService.DownloadItemsAsync(
                    selected,
                    d => d.IsSelected,
                    root,
                    3,
                    db,
                    null,
                    CancellationToken.None);
                await DisplayAlert("Done", "Downloads completed", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Download failed", ex.Message, "OK");
            }
        }
    }
}
