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

To publish a self-contained build from Visual Studio:

1. Right-click **BoothDownloadApp** and choose **Publish**.
2. Select **Folder** as the target and create the profile.
3. Set **Deployment Mode** to **Self-contained** and pick the runtime (e.g. `win-x64`).
4. Click **Publish**. The `publish` folder will contain `BoothDownloadApp.exe` and all required files.

## WPF Application Usage

1. Launch `BoothDownloadApp.exe` from the build output or run `dotnet run --project BoothDownloadApp/BoothDownloadApp.csproj`.
2. The Booth login page opens in your default browser so you can sign in if needed.
3. Click **"📥 JSON 読み込み"** and choose your `booth_data.json` file.
4. Choose a download folder with **"📂選択"** and start downloading with **"⬇️ ダウンロード開始"**. Links open in your browser and finished files are moved automatically to the chosen folder. Use **"⏸ 停止"** to cancel.
   To download all undownloaded files at once, press **"⬇️ 未DL一括"**.
5. Files are organized by shop and product name and also copied to any configured favorite folders. The app keeps management data in `booth_manage.json` next to the executable.
6. Use the filter panel to narrow items by tag, hide downloaded items or show only those with updates. A search box lets you filter by keyword across names, shops and tags.
7. Use **"＋ 手動追加"** to register items yourself. Pasting a product URL automatically fetches the name, shop and tags. You can still press **"情報取得"** to retry or type details manually. Local files may be attached and copied into your download folder.

## Chrome Extension Usage

1. Open `chrome://extensions`, enable **Developer mode**, and choose **Load unpacked**. Select the `booth-scraper-extension` directory.
2. While logged in to Booth, visit your library page
   (`https://accounts.booth.pm/library` or `https://booth.pm/library`).
   You can also start the extension from the gifts page
   (`https://accounts.booth.pm/library/gifts`).
   If you see an error about opening the page, ensure you are logged in and on the correct page.
3. Click the extension icon. It scrapes all pages of your library and gifts and downloads `booth_library.json`.
4. Use the **"📥 JSON 読み込み"** button in the WPF app to select the downloaded file. Gift entries are also imported.

