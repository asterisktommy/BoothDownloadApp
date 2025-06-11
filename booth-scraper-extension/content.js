(async () => {
  if (!window.boothScraper) {
    console.error('scraper library missing');
    return;
  }
  const data = await window.boothScraper.scrapeLibrary();
  window.boothScraper.downloadJson(data);
})();
