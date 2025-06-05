# Booth Download App

A Windows WPF application and a small Chrome extension for managing downloads from [Booth](https://booth.pm/).

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- Windows is required to run the WPF application

## Building

From the repository root run:

```bash
dotnet build BoothDownloadApp.sln
```

The resulting executable will be under `BoothDownloadApp/bin/Debug/net8.0-windows/` (or `Release` depending on configuration).

## WPF Application Usage

1. Launch `BoothDownloadApp.exe` from the build output or run `dotnet run --project BoothDownloadApp/BoothDownloadApp.csproj`.
2. Click **"📥 JSON 読み込み"** to import `booth_data.json`. The app looks for this file in your `Downloads` folder and copies it to `C:\BoothData`.
3. Choose a download folder with **"📂選択"** and start downloading with **"⬇️ ダウンロード開始"**. Use **"⏸ 停止"** to cancel.
4. Downloaded files are organized under the selected folder by shop and product name. The app keeps management data in `booth_manage.json` next to the executable.
5. Use the filter panel to narrow items by tag, hide downloaded items or show only those with updates.

## Chrome Extension Usage

1. Open `chrome://extensions`, enable **Developer mode**, and choose **Load unpacked**. Select the `booth-scraper-extension` directory.
2. While logged in to Booth, visit your library page
   (`https://accounts.booth.pm/library` or `https://booth.pm/library`).
   You can also start the extension from the gifts page
   (`https://accounts.booth.pm/library/gifts`).
   If you see an error about opening the page, ensure you are logged in and on the correct page.
3. Click the extension icon. It scrapes all pages of your library and gifts and downloads `booth_library.json`.
4. Move or rename this file to `booth_data.json` in your `Downloads` folder so the WPF app can load it. Gift entries are also imported.

