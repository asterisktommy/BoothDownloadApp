(async () => {
  if (!window.boothScraper) {
    console.error('scraper library missing');
    return;
  }
  const data = await window.boothScraper.scrapeLibrary();
  window.boothScraper.downloadJson(data);
  if (typeof chrome !== 'undefined' && chrome.runtime) {
    chrome.runtime.sendMessage({ action: 'complete' });
  }
})();
