# Booth Library Scraper Chrome Extension

This Chrome extension scrapes the logged in Booth library pages and outputs
all purchase and gift information to a JSON file.

## Usage
1. Load the extension from this folder (`chrome://extensions`, enable Developer Mode,
   then "Load unpacked").
2. Navigate to your Booth library page after logging in
   (usually `https://accounts.booth.pm/library`).
   The extension also works when started from the gifts page
   (`https://accounts.booth.pm/library/gifts`).
   If you see an error saying to open the page, make sure you are logged in and on the correct page.
3. Click the extension icon to start scraping. It will fetch every page of the
   library and gift sections and then download `booth_library.json`.
   Item tags are fetched concurrently for faster completion. The scraping
   logic lives in `scraper.js` which keeps the injected `content.js` minimal.
   The scripts are injected into the main page context so tag fetching can reuse
   your logged in session cookies without CORS errors. Each item's details are
   requested from its own domain so tags load correctly regardless of subdomain.

The produced JSON file contains two arrays:

```json
{
  "library": [ /* purchased items */ ],
  "gifts": [ /* gift items */ ]
}
```
Each item includes `productName`, `shopName`, `thumbnail` and an array of
`downloads` with `fileName` and `downloadLink`.
