# Booth Library Scraper Chrome Extension

This Chrome extension scrapes the logged in Booth library pages and outputs
all purchase and gift information to a JSON file.

## Usage
1. Load the extension from this folder (`chrome://extensions`, enable Developer Mode,
   then "Load unpacked").
2. Navigate to your Booth library page after logging in
   (`https://accounts.booth.pm/library` or `https://booth.pm/library`).
   The extension also works when started from the gifts page
   (`https://accounts.booth.pm/library/gifts`).
   If you see an error saying to open the page, make sure you are logged in and on the correct page.
3. Click the extension icon to start scraping. It will fetch every page of the
   library and gift sections and then download `booth_library.json`.
   This extension no longer retrieves item tags. Tags are fetched automatically
   inside the desktop app where you can filter and search by them.

The produced JSON file contains two arrays:

```json
{
  "library": [ /* purchased items */ ],
  "gifts": [ /* gift items */ ]
}
```
Each item includes `productName`, `shopName`, `thumbnail` and an array of
`downloads` with `fileName` and `downloadLink`.
