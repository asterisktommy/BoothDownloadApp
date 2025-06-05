# Booth Library Scraper Chrome Extension

This Chrome extension scrapes the logged in Booth library pages and outputs
all purchase and gift information to a JSON file.

## Usage
1. Load the extension from this folder (`chrome://extensions`, enable Developer Mode,
   then "Load unpacked").
2. Navigate to your Booth library page after logging in
   (`https://accounts.booth.pm/library` or `https://booth.pm/library`).
3. Click the extension icon to start scraping. It will fetch every page of the
   library and gift sections and then download `booth_library.json`.

The produced JSON file contains two arrays:

```json
{
  "library": [ /* purchased items */ ],
  "gifts": [ /* gift items */ ]
}
```
Each item includes `productName`, `shopName`, `thumbnail` and an array of
`downloads` with `fileName` and `downloadLink`.
