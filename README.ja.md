# Booth ダウンロードアプリ

Windows 用 WPF アプリケーションと、Booth からのダウンロード管理を行う小さな Chrome 拡張機能です。

## 前提条件

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- WPF アプリの実行には Windows が必要です

## ビルド方法

リポジトリのルートで次を実行します:

```bash
dotnet build BoothDownloadApp.sln
```

生成された実行ファイルは `BoothDownloadApp/bin/Debug/net8.0-windows/` (または `Release`)
以下に出力されます。

Visual Studio から自己完結型ビルドを発行するには:

1. **BoothDownloadApp** を右クリックし **発行** を選択します。
2. **フォルダー** をターゲットとしてプロファイルを作成します。
3. **配置モード** を **自己完結型** に設定し、ランタイム (例: `win-x64`) を選びます。
4. **発行** をクリックすると `publish` フォルダーに `BoothDownloadApp.exe` と必要なファイルが生成されます。

## WPF アプリの使い方

1. ビルドした `BoothDownloadApp.exe` を起動するか、`dotnet run --project BoothDownloadApp/BoothDownloadApp.csproj` を実行します。
2. **"📥 JSON 読み込み"** をクリックして `booth_data.json` を選択します。
3. **"📂選択"** でダウンロード先フォルダーを指定し、**"⬇️ ダウンロード開始"** で開始します。**"⏸ 停止"** でキャンセルできます。
   未ダウンロードのファイルを一括で取得するには **"⬇️ 未DL一括"** を押してください。
4. ダウンロードされたファイルは選択したフォルダー内でショップと商品名ごとに整理されます。アプリは実行ファイルと同じ場所に `booth_manage.json` を保存し、管理情報を保持します。
5. フィルターパネルでタグによる絞り込み、ダウンロード済みの非表示、更新がある項目のみ表示が行えます。検索ボックスでは名前・ショップ・タグを横断してキーワード検索が可能です。
6. **"＋ 手動追加"** を使うと手動でアイテムを登録できます。商品 URL を貼り付けると自動で名前・ショップ・タグを取得し、**"情報取得"** で再取得もできます。ローカルファイルを添付してダウンロードフォルダーにコピーすることも可能です。

## Chrome 拡張機能の使い方

1. `chrome://extensions` を開き、**デベロッパーモード** を有効にして **パッケージ化されていない拡張機能を読み込む** を選択します。`booth-scraper-extension` ディレクトリを指定してください。
2. Booth にログインした状態でライブラリページ
   (`https://accounts.booth.pm/library` または `https://booth.pm/library`) を開きます。
   ギフトページ (`https://accounts.booth.pm/library/gifts`) からも起動できます。
   ページを開けない場合はログイン状態と URL を確認してください。
3. 拡張機能のアイコンをクリックすると、ライブラリとギフトの全ページをスクレイプし `booth_library.json` をダウンロードします。
4. WPF アプリの **"📥 JSON 読み込み"** ボタンからダウンロードしたファイルを選択してください。ギフトも取り込まれます。
